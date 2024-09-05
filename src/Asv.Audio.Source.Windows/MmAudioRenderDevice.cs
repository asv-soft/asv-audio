using System.Buffers;
using System.Reactive.Subjects;
using Asv.Common;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Asv.Audio.Source.Windows;

public class MmAudioRenderDevice : DisposableOnceWithCancel, IAudioRenderDevice
{
    private readonly Subject<Memory<byte>> _onData;
    private readonly BufferedWaveProvider _playBuffer;
    private readonly WasapiOut _waveOut;
    

    public MmAudioRenderDevice(MMDevice device, AudioFormat format,AudioClientShareMode mode = AudioClientShareMode.Exclusive, bool useEventSync = true, int latency = 60 )
    {
        Format = format;
        _onData = new Subject<Memory<byte>>().DisposeItWith(Disposable);
        _playBuffer = new BufferedWaveProvider(new WaveFormat(format.SampleRate, format.Bits, format.Channel));
        _playBuffer.DiscardOnBufferOverflow = true;
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