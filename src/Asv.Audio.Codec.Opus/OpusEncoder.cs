using System.Buffers;
using System.Reactive.Subjects;
using Asv.Common;

namespace Asv.Audio.Codec.Opus;

public class OpusEncoder: DisposableOnceWithCancel, IObservable<ReadOnlyMemory<byte>>
{
    private readonly int _frameSize;
    private readonly IntPtr _encoder;
    private const int OpusBitrate = 16;
    private const int MaxEncodedBytes = 4000;
    private readonly Subject<ReadOnlyMemory<byte>> _outputSubject;
    private readonly byte[] _outBuffer;
    private readonly Memory<byte> _outMemory;


    public OpusEncoder(IObservable<ReadOnlyMemory<byte>> src, AudioFormat pcmFormat, OpusApplication app = OpusApplication.Voip, bool forwardErrorCorrection = false, int frameSize = 960, int codecBitrate = 8000, bool useArrayPool = true)
    {
        _frameSize = frameSize;
        _outputSubject = new Subject<ReadOnlyMemory<byte>>().DisposeItWith(Disposable);
        if (pcmFormat.SampleRate != 8000 &&
            pcmFormat.SampleRate != 12000 &&
            pcmFormat.SampleRate != 16000 &&
            pcmFormat.SampleRate != 24000 &&
            pcmFormat.SampleRate != 48000)
            throw new ArgumentOutOfRangeException(nameof(pcmFormat.SampleRate));
        if (pcmFormat.Channel != 1 && pcmFormat.Channel != 2)
            throw new ArgumentOutOfRangeException(nameof(pcmFormat.Channel));
        if (pcmFormat.Bits != 16)
            throw new ArgumentOutOfRangeException(nameof(pcmFormat.Bits)); // TODO: check for 8 bits
        
        _encoder = OpusNative.opus_encoder_create(pcmFormat.SampleRate, pcmFormat.Channel, (int)app, out var error);
        if ((Errors)error != Errors.OK)
        {
            throw new Exception($"Exception occured while creating opus encoder:{(Errors)error:G}");
        }
        CheckError(OpusNative.opus_encoder_ctl(_encoder, OpusCtl.OpusSetInbandFecRequest, forwardErrorCorrection ? 1 : 0));
        CheckError(OpusNative.opus_encoder_ctl(_encoder, OpusCtl.OpusSetBitrateRequest, codecBitrate));
        
        
        var chunkByteSize = _frameSize * pcmFormat.BytesPerSample;
        src.Chunking(chunkByteSize , useArrayPool).Subscribe(Encode).DisposeItWith(Disposable);
        
        if (useArrayPool)
        {
            _outBuffer = ArrayPool<byte>.Shared.Rent(MaxEncodedBytes);
            Disposable.AddAction(() => ArrayPool<byte>.Shared.Return(_outBuffer));
        }
        else
        {
            _outBuffer = new byte[MaxEncodedBytes];
        }
        _outMemory = new Memory<byte>(_outBuffer, 0, MaxEncodedBytes);
    }

    private void Encode(ReadOnlyMemory<byte> input)
    {
        if (IsDisposed) return;
        using var inputHandle = input.Pin();
        using var outputHandle = _outMemory.Pin();
        int length;
        unsafe
        {
            length = OpusNative.opus_encode(_encoder, inputHandle.Pointer, _frameSize,
                outputHandle.Pointer, MaxEncodedBytes);
        }
        CheckError(length);
        _outputSubject.OnNext(new ReadOnlyMemory<byte>(_outBuffer, 0, length));
    }

    private void CheckError(int result)
    {
        if (result < 0)
            throw new Exception($"Encoding failed - {(Errors)result:G}" );
    }

    public IDisposable Subscribe(IObserver<ReadOnlyMemory<byte>> observer)
    {
        return _outputSubject.Subscribe(observer);
    }
}