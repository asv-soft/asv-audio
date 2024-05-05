using System.Reactive.Subjects;
using Asv.Common;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Asv.Audio.Source.Windows;

internal class MmAudioCaptureDevice : DisposableOnceWithCancel, IAudioCaptureDevice
{
    private readonly WasapiCapture _waveIn;
    private readonly Subject<ReadOnlyMemory<byte>> _onData;

    public MmAudioCaptureDevice(MMDevice device,AudioFormat format)
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