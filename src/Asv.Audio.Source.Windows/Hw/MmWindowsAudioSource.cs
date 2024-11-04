using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Asv.Common;
using DynamicData;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NLog;

namespace Asv.Audio.Source.Windows;

public class MmWindowsAudioSource : DisposableOnceWithCancel, IAudioSource, IMMNotificationClient
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly SourceCache<IAudioDeviceInfo, string> _recordDeviceSource;
    private readonly SourceCache<IAudioDeviceInfo, string> _playDeviceSource;
    private readonly MMDeviceEnumerator _enumerator;
    private int _refreshIsBusy;
    private readonly Subject<Unit> _refreshSubject;

    public MmWindowsAudioSource(string id = "WndMM")
    {
        Id = id;
        _recordDeviceSource = new SourceCache<IAudioDeviceInfo, string>(x => x.Id).DisposeItWith(
            Disposable
        );
        CaptureDevices = _recordDeviceSource.Connect().RefCount();
        _playDeviceSource = new SourceCache<IAudioDeviceInfo, string>(x => x.Id).DisposeItWith(
            Disposable
        );
        RenderDevices = _playDeviceSource.Connect().RefCount();
        _enumerator = new MMDeviceEnumerator().DisposeItWith(Disposable);
        _enumerator.RegisterEndpointNotificationCallback(this);
        _refreshSubject = new Subject<Unit>().DisposeItWith(Disposable);
        _refreshSubject
            .Throttle(TimeSpan.FromMilliseconds(1000))
            .Subscribe(RefreshDevices)
            .DisposeItWith(Disposable);
        RefreshDevices(Unit.Default);
    }

    public string Id { get; }
    public IObservable<IChangeSet<IAudioDeviceInfo, string>> CaptureDevices { get; }

    public IAudioCaptureDevice? CreateCaptureDevice(string deviceId, AudioFormat format)
    {
        return new MmAudioCaptureDevice(_enumerator.GetDevice(deviceId), format);
    }

    public IObservable<IChangeSet<IAudioDeviceInfo, string>> RenderDevices { get; }

    public IAudioRenderDevice? CreateRenderDevice(string deviceId, AudioFormat format) =>
        new MmAudioRenderDevice(_enumerator.GetDevice(deviceId), format);

    private void RefreshDevices(Unit unit)
    {
        if (Interlocked.CompareExchange(ref _refreshIsBusy, 1, 0) != 0)
        {
            return;
        }

        try
        {
            _recordDeviceSource.Edit(inner =>
            {
                inner.Clear();
                inner.AddOrUpdate(
                    _enumerator
                        .EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
                        .Select(x => new AudioDeviceInfo(x.ID, x.FriendlyName, this))
                );
            });
            _playDeviceSource.Edit(inner =>
            {
                inner.Clear();
                inner.AddOrUpdate(
                    _enumerator
                        .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                        .Select(x => new AudioDeviceInfo(x.ID, x.FriendlyName, this))
                );
            });
        }
        catch (Exception e)
        {
            Logger.Error(e, "Error on refresh devices: {message}", e.Message);
        }
        finally
        {
            Interlocked.Exchange(ref _refreshIsBusy, 0);
        }
    }

    #region NotificationClient

    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
        Logger.Info("DeviceStateChanged {deviceId} {newState}", deviceId, newState);
        _refreshSubject.OnNext(Unit.Default);
    }

    public void OnDeviceAdded(string pwstrDeviceId)
    {
        Logger.Info("DeviceAdded {deviceId}", pwstrDeviceId);
        _refreshSubject.OnNext(Unit.Default);
    }

    public void OnDeviceRemoved(string deviceId)
    {
        Logger.Info("DeviceRemoved {deviceId}", deviceId);
        _refreshSubject.OnNext(Unit.Default);
    }

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        Logger.Info(
            "DefaultDeviceChanged {flow} {role} {defaultDeviceId}",
            flow,
            role,
            defaultDeviceId
        );
        _refreshSubject.OnNext(Unit.Default);
    }

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
    {
        Logger.Info("PropertyValueChanged {deviceId} {key}", pwstrDeviceId, key);
        _refreshSubject.OnNext(Unit.Default);
    }

    #endregion
}
