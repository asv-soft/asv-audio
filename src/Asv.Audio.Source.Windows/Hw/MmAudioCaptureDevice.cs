using Asv.Common;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using R3;

namespace Asv.Audio.Source.Windows;

internal class MmAudioCaptureDevice : AsyncDisposableWithCancel, IAudioCaptureDevice
{
    private readonly WasapiCapture _waveIn;
    private readonly Subject<ReadOnlyMemory<byte>> _onData = new();
    private readonly IDisposable _sub1;

    public MmAudioCaptureDevice(MMDevice device,AudioFormat format)
    {
        Format = format;
        _waveIn = new WasapiCapture(device);
        _waveIn.WaveFormat = new WaveFormat(format.SampleRate, format.Bits, format.Channel);
        _sub1 = Observable.FromEventHandler<WaveInEventArgs>(
                h => _waveIn.DataAvailable += h, 
                h => _waveIn.DataAvailable -= h)
            .Select(args=>new ReadOnlyMemory<byte>(args.e.Buffer,0,args.e.BytesRecorded))
            .Subscribe(_onData.AsObserver());
    }

    public Observable<ReadOnlyMemory<byte>> Output => _onData;

    public void Start()
    {
        _waveIn.StartRecording();
    }

    public void Stop()
    {
        _waveIn.StopRecording();
    }

    public AudioFormat Format { get; }

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _waveIn.Dispose();
            _onData.Dispose();
            _sub1.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await CastAndDispose(_waveIn);
        await CastAndDispose(_onData);
        await CastAndDispose(_sub1);

        await base.DisposeAsyncCore();

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
            {
                await resourceAsyncDisposable.DisposeAsync();
            }
            else
            {
                resource.Dispose();
            }
        }
    }

    #endregion
}