using System.Reactive.Subjects;
using Asv.Common;

namespace Asv.Audio.Codec.Opus;

public class OpusDecoder: DisposableOnceWithCancel, IAudioRenderSubject
{
    private readonly IntPtr _decoder;
    private readonly int _bytesPerSample;
    private readonly int _frameSize;
    private const int OpusBitrate = 16;
    private const int MaxDecodedSize = 4000;
    private readonly Subject<ReadOnlyMemory<byte>> _inputData;
    private readonly ChunkingSubject<byte> _chunkedData;

    public OpusDecoder(IAudioRenderSubject src, OpusApplication app, bool forwardErrorCorrection, int segmentFrames = 960, bool useArrayPool = true)
    {
        _inputData = new Subject<ReadOnlyMemory<byte>>().DisposeItWith(Disposable);
        Format = src.Format;
        if (Format.SampleRate != 8000 &&
            Format.SampleRate != 12000 &&
            Format.SampleRate != 16000 &&
            Format.SampleRate != 24000 &&
            Format.SampleRate != 48000)
            throw new ArgumentOutOfRangeException(nameof(Format.SampleRate));
        if (Format.Channel != 1 && Format.Channel != 2)
            throw new ArgumentOutOfRangeException(nameof(Format.Channel));
        _decoder = OpusNative.opus_decoder_create(Format.SampleRate, Format.Channel, out var error);
        if ((Errors)error != Errors.OK)
        {
            throw new Exception("Exception occured while creating decoder");
        }
        var bytesPerSample = (OpusBitrate / 8) * Format.Channel;
        _frameSize = MaxDecodedSize / bytesPerSample;
        _chunkedData = new ChunkingSubject<byte>(_inputData, segmentFrames * 2 * Format.Channel , useArrayPool).DisposeItWith(Disposable);
        _chunkedData.Subscribe(Decode).DisposeItWith(Disposable);
        
    }

    private void Decode(ReadOnlyMemory<byte> data)
    {
        
    }

    private void CheckError(int result)
    {
        if (result < 0)
            throw new Exception($"Encoding failed - {(Errors)result:G}" );
    }

    public void OnCompleted()
    {
        _inputData.OnCompleted();
    }

    public void OnError(Exception error)
    {
        _inputData.OnError(error);
    }

    public void OnNext(ReadOnlyMemory<byte> value)
    {
        _inputData.OnNext(value);
    }

    public AudioFormat Format { get; }
}