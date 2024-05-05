using Asv.Common;
using DynamicData;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NLog;

namespace Asv.Audio.Source.Windows;



public class MmWindowsAudioSource:DisposableOnceWithCancel, IAudioSource, IMMNotificationClient
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly SourceCache<IAudioDeviceInfo,string> _recordDeviceSource;
    private readonly SourceCache<IAudioDeviceInfo,string> _playDeviceSource;
    private readonly MMDeviceEnumerator _enumerator;
    private int _refreshIsBusy;

    public MmWindowsAudioSource()
    {
        _recordDeviceSource = new SourceCache<IAudioDeviceInfo, string>(x => x.Id).DisposeItWith(Disposable);
        CaptureDevices = _recordDeviceSource.Connect().RefCount();
        _playDeviceSource = new SourceCache<IAudioDeviceInfo, string>(x => x.Id).DisposeItWith(Disposable);
        RenderDevices = _playDeviceSource.Connect().RefCount();
        _enumerator = new MMDeviceEnumerator().DisposeItWith(Disposable);
        _enumerator.RegisterEndpointNotificationCallback(this);
        RefreshDevices();
    }

    public IObservable<IChangeSet<IAudioDeviceInfo, string>> CaptureDevices { get; } 
    public IAudioCaptureDevice CreateCaptureDevice(string deviceId, AudioFormat format) => new MmAudioCaptureDevice(_enumerator.GetDevice(deviceId),format);
    public IObservable<IChangeSet<IAudioDeviceInfo, string>> RenderDevices { get; }
    public IAudioRenderDevice CreateRenderDevice(string deviceId, AudioFormat format) => new MmAudioRenderDevice(_enumerator.GetDevice(deviceId),format);

    private void RefreshDevices()
    {
        if (Interlocked.CompareExchange(ref _refreshIsBusy,1,0) != 0) return;
        try
        {
            _recordDeviceSource.Edit(inner =>
            {
                inner.Clear();
                inner.AddOrUpdate(_enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
                    .Select(x => new AudioDeviceInfo(x.ID, x.FriendlyName)));
            });
            _playDeviceSource.Edit(inner =>
            {
                inner.Clear();
                inner.AddOrUpdate(_enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                    .Select(x => new AudioDeviceInfo(x.ID, x.FriendlyName)));
            });
        }
        catch (Exception e)
        {
            Logger.Error(e,"Error on refresh devices: {message}",e.Message);
        }
        finally
        {
            Interlocked.Exchange(ref _refreshIsBusy, 0);
        }
        
    }
    
    #region NotificationClient
    
    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
        Logger.Info("DeviceStateChanged {deviceId} {newState}",deviceId,newState);
        RefreshDevices();
    }

    public void OnDeviceAdded(string pwstrDeviceId)
    {
        Logger.Info("DeviceAdded {deviceId}",pwstrDeviceId);
        RefreshDevices();
    }

    public void OnDeviceRemoved(string deviceId)
    {
        Logger.Info("DeviceRemoved {deviceId}",deviceId);
        RefreshDevices();
    }

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        Logger.Info("DefaultDeviceChanged {flow} {role} {defaultDeviceId}",flow,role,defaultDeviceId);
        RefreshDevices();
    }

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
    {
        Logger.Info("PropertyValueChanged {deviceId} {key}",pwstrDeviceId,key);
        RefreshDevices();
    }
    
    #endregion
}