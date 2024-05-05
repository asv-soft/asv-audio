using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using Asv.Common;

namespace Asv.Audio.Codec.Opus;

public class OpusDecoder: DisposableOnceWithCancel, IObservable<ReadOnlyMemory<byte>>
{
    private readonly IntPtr _decoder;
    private readonly int _bytesPerSample;
    private readonly int _frameSize;
    private const int OpusBitrate = 16;
    private const int MaxDecodedSize = 4000;
    private readonly Subject<ReadOnlyMemory<byte>> _inputData;
    

    public OpusDecoder(IObservable<ReadOnlyMemory<byte>> src, AudioFormat pcmFormat, OpusApplication app, bool forwardErrorCorrection, int segmentFrames = 960, bool useArrayPool = true)
    {
        _inputData = new Subject<ReadOnlyMemory<byte>>().DisposeItWith(Disposable);
        if (pcmFormat.SampleRate != 8000 &&
            pcmFormat.SampleRate != 12000 &&
            pcmFormat.SampleRate != 16000 &&
            pcmFormat.SampleRate != 24000 &&
            pcmFormat.SampleRate != 48000)
            throw new ArgumentOutOfRangeException(nameof(pcmFormat.SampleRate));
        if (pcmFormat.Channel != 1 && pcmFormat.Channel != 2)
            throw new ArgumentOutOfRangeException(nameof(pcmFormat.Channel));
        _decoder = OpusNative.opus_decoder_create(pcmFormat.SampleRate, pcmFormat.Channel, out var error);
        if ((Errors)error != Errors.OK)
        {
            throw new Exception("Exception occured while creating decoder");
        }
        var bytesPerSample = OpusBitrate / 8 * pcmFormat.Channel;
        src.Chunking(segmentFrames * 2 * pcmFormat.Channel , useArrayPool).Subscribe(Decode).DisposeItWith(Disposable);
        _frameSize = MaxDecodedSize / bytesPerSample;
        
    }

    private void Decode(ReadOnlyMemory<byte> input)
    {
        if (IsDisposed) return;
        using var inputHandle = input.Pin();
        using var outputHandle = output.Pin();
        
    }

    private void CheckError(int result)
    {
        if (result < 0)
            throw new Exception($"Encoding failed - {(Errors)result:G}" );
    }

    public IDisposable Subscribe(IObserver<ReadOnlyMemory<byte>> observer)
    {
        throw new NotImplementedException();
    }
}