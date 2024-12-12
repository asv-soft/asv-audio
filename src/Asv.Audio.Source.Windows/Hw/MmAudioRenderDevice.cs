using System.Buffers;
using Asv.Common;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using R3;

namespace Asv.Audio.Source.Windows;

public class MmAudioRenderDevice : AsyncDisposableWithCancel, IAudioRenderDevice
{
    private readonly Subject<ReadOnlyMemory<byte>> _onData = new();
    private readonly BufferedWaveProvider _playBuffer;
    private readonly WasapiOut _waveOut;
    private readonly IDisposable _sub1;


    public MmAudioRenderDevice(MMDevice device, AudioFormat format,AudioClientShareMode mode = AudioClientShareMode.Exclusive, bool useEventSync = true, int latency = 60 )
    {
        Format = format;
        _playBuffer = new BufferedWaveProvider(new WaveFormat(format.SampleRate, format.Bits, format.Channel))
        {
            DiscardOnBufferOverflow = true,
        };
        _waveOut = new WasapiOut(device,mode, useEventSync, latency);
        _waveOut.Init(_playBuffer);
        _sub1 = _onData.Subscribe(OnNext);
    }

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

    public Observer<ReadOnlyMemory<byte>> Input => _onData.AsObserver();

    public void Start()
    {
        _waveOut.Play();
    }

    public void Stop()
    {
        _waveOut.Stop();
    }

    public AudioFormat Format { get; }

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _onData.Dispose();
            _waveOut.Dispose();
            _sub1.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await CastAndDispose(_onData);
        await CastAndDispose(_waveOut);
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