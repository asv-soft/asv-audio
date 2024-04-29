using System.Buffers;
using System.Reactive.Subjects;
using Asv.Common;
using DynamicData;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using NLog;

namespace Asv.Audio.Source.Windows;



public class WindowsAudioSource:DisposableOnceWithCancel, IAudioSource, IMMNotificationClient
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly SourceCache<IAudioDeviceInfo,string> _recordDeviceSoruce;
    private readonly SourceCache<IAudioDeviceInfo,string> _playDeviceSource;
    private readonly MMDeviceEnumerator _enumerator;
    private int _refreshIsBusy;


    public WindowsAudioSource()
    {
        _recordDeviceSoruce = new SourceCache<IAudioDeviceInfo, string>(x => x.Id).DisposeItWith(Disposable);
        CaptureDevices = _recordDeviceSoruce.Connect().RefCount();
        _playDeviceSource = new SourceCache<IAudioDeviceInfo, string>(x => x.Id).DisposeItWith(Disposable);
        RenderDevices = _playDeviceSource.Connect().RefCount();
        _enumerator = new MMDeviceEnumerator().DisposeItWith(Disposable);
        _enumerator.RegisterEndpointNotificationCallback(this);
        RefreshDevices();

    }

    public IObservable<IChangeSet<IAudioDeviceInfo, string>> CaptureDevices { get; } 
    public IAudioCaptureDevice CreateCaptureDevice(string deviceId, AudioFormat format) => new AudioCaptureDevice(_enumerator.GetDevice(deviceId),format);
    public IObservable<IChangeSet<IAudioDeviceInfo, string>> RenderDevices { get; }
    public IAudioRenderDevice CreateRenderDevice(string deviceId, AudioFormat format) => new AudioRenderDevice(_enumerator.GetDevice(deviceId),format);

    private void RefreshDevices()
    {
        if (Interlocked.CompareExchange(ref _refreshIsBusy,1,0) != 0) return;
        try
        {
            var devices = _enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
                .Select(x => new AudioDeviceInfo(x.ID, x.FriendlyName)).ToArray();
            _recordDeviceSoruce.Edit(inner =>
            {
                inner.Clear();
                inner.AddOrUpdate(devices);
            });
            devices = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                .Select(x => new AudioDeviceInfo(x.ID, x.FriendlyName)).ToArray();
            _playDeviceSource.Edit(inner =>
            {
                inner.Clear();
                inner.AddOrUpdate(devices);
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
    
    #region IMMNotificationClient
    
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

public class AudioRenderDevice : DisposableOnceWithCancel, IAudioRenderDevice
{
    private readonly Subject<Memory<byte>> _onData;
    private readonly BufferedWaveProvider _playBuffer;
    private readonly WasapiOut _waveOut;
    

    public AudioRenderDevice(MMDevice device, AudioFormat format,AudioClientShareMode mode = AudioClientShareMode.Exclusive, bool useEventSync = true, int latency = 60 )
    {
        Format = format;
        _onData = new Subject<Memory<byte>>().DisposeItWith(Disposable);
        _playBuffer = new BufferedWaveProvider(new WaveFormat(format.SampleRate, format.Bits, format.Channel));
        _waveOut = new WasapiOut(device,mode, useEventSync, latency).DisposeItWith(Disposable);
        _waveOut.Init(_playBuffer);
    }

    public void OnCompleted() => _onData.OnCompleted();

    public void OnError(Exception error) => _onData.OnError(error);

    public void OnNext(ReadOnlyMemory<byte> value)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(value.Length);
        try
        {
            value.CopyTo(new Memory<byte>(buffer,0,value.Length));
            _playBuffer.AddSamples(buffer, 0, value.Length);    
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public void Start()
    {
        _waveOut.Play();
    }

    public void Stop()
    {
        _waveOut.Stop();
    }

    public AudioFormat Format { get; }
}

internal class AudioCaptureDevice : DisposableOnceWithCancel, IAudioCaptureDevice
{
    private readonly WasapiCapture _waveIn;
    private readonly Subject<ReadOnlyMemory<byte>> _onData;

    public AudioCaptureDevice(MMDevice device,AudioFormat format)
    {
        Format = format;
        _onData = new Subject<ReadOnlyMemory<byte>>().DisposeItWith(Disposable);
        _waveIn = new WasapiCapture(device).DisposeItWith(Disposable);
        _waveIn.WaveFormat = new WaveFormat(format.SampleRate, format.Bits, format.Channel);
        _waveIn.DataAvailable += OnDataAvailable;
        Disposable.AddAction(() => _waveIn.DataAvailable -= OnDataAvailable);
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (IsDisposed) return;
        _onData.OnNext(new ReadOnlyMemory<byte>(e.Buffer,0,e.BytesRecorded));
    }

    public void Start()
    {
        _waveIn.StartRecording();
    }

    public void Stop()
    {
        _waveIn.StopRecording();
    }

    public IDisposable Subscribe(IObserver<ReadOnlyMemory<byte>> observer)
    {
        return _onData.Subscribe(observer);
    }

    public AudioFormat Format { get; }
}

internal class AudioDeviceInfo(string id, string friendlyName) : IAudioDeviceInfo
{
    public string Id { get; } = id;
    public string Name { get; } = friendlyName;
}

