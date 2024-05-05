using System.Buffers;
using System.Reactive.Subjects;
using Asv.Common;

namespace Asv.Audio.Codec.Opus;

public class OpusDecoder: DisposableOnceWithCancel, IObservable<ReadOnlyMemory<byte>>
{
    private readonly int _frameSize;
    private const int OpusBitrate = 16;
    private const int MaxDecodedSize = 8000;
    private readonly Subject<ReadOnlyMemory<byte>> _outputSubject;
    private readonly byte[] _outBuffer;
    private readonly Memory<byte> _outMemory;
    private readonly int _forwardErrorCorrection;
    private readonly IntPtr _decoder;


    public OpusDecoder(IObservable<ReadOnlyMemory<byte>> src, AudioFormat pcmFormat, bool forwardErrorCorrection = false, bool useArrayPool = true)
    {
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
        
        _forwardErrorCorrection = forwardErrorCorrection ? 1 : 0;
        _decoder = OpusNative.opus_decoder_create(pcmFormat.SampleRate, pcmFormat.Channel, out var error);
        if ((Errors)error != Errors.OK)
        {
            throw new Exception($"Exception occured while creating opus decoder:{(Errors)error:G}");
        }
       
        var bytesPerSample = (OpusBitrate / 8) * pcmFormat.Channel;
        _frameSize = MaxDecodedSize / bytesPerSample;
        
        src.Subscribe(Decode).DisposeItWith(Disposable);
        
        if (useArrayPool)
        {
            _outBuffer = ArrayPool<byte>.Shared.Rent(MaxDecodedSize);
            Disposable.AddAction(() => ArrayPool<byte>.Shared.Return(_outBuffer));
        }
        else
        {
            _outBuffer = new byte[MaxDecodedSize];
        }
        _outMemory = new Memory<byte>(_outBuffer, 0, MaxDecodedSize);
    }

    private void Decode(ReadOnlyMemory<byte> input)
    {
        if (IsDisposed) return;
        using var inputHandle = input.Pin();
        using var outputHandle = _outMemory.Pin();
        int length;
        unsafe
        {
            length = OpusNative.opus_decode(_decoder,inputHandle.Pointer, input.Length, outputHandle.Pointer, _frameSize, _forwardErrorCorrection);
        }
        CheckError(length);
        _outputSubject.OnNext(new ReadOnlyMemory<byte>(_outBuffer, 0, length * 2));
    }

    private void CheckError(int result)
    {
        if (result < 0)
            throw new Exception($"Decoding failed - {(Errors)result:G}" );
    }

    public IDisposable Subscribe(IObserver<ReadOnlyMemory<byte>> observer)
    {
        return _outputSubject.Subscribe(observer);
    }
}