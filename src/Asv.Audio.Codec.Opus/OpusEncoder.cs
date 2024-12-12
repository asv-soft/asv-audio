using System.Buffers;
using Asv.Common;
using R3;

namespace Asv.Audio.Codec.Opus;

public class OpusEncoderSettings
{
    public const int DefaultFrameSize = 960 * 6; // 120 ms

    /// <summary>
    /// Gets or sets the application mode for the Opus encoder, which determines the encoding behavior and optimizations.
    /// </summary>
    public OpusApplication Application { get; set; } = OpusApplication.OpusVoip;

    /// <summary>
    /// Gets or sets a value indicating whether Forward Error Correction (FEC) is enabled for the Opus encoder,
    /// allowing improved packet loss resiliency by embedding redundant information within the stream.
    /// </summary>
    public bool ForwardErrorCorrection { get; set; } = true;

    /// <summary>
    /// Gets or sets the frame size for audio processing in the Opus encoder, which determines the number of audio samples
    /// per frame. This value directly impacts the latency and encoding granularity.
    /// </summary>
    public int FrameSize { get; set; } = DefaultFrameSize;

    /// <summary>
    /// Gets or sets the target bitrate for the Opus encoder, which determines the quality and compression ratio of the encoded audio.
    /// </summary>
    public int CodecBitrate { get; set; } = 6000;

    /// <summary>
    /// Gets or sets the bandwidth for the Opus encoder, which determines the frequency range and audio quality of the encoded audio.
    /// </summary>
    public OpusBandwidth Bandwidth { get; set; } = OpusBandwidth.OpusNarrowband;

    /// <summary>
    /// Gets or sets the signal type for the Opus encoder, which determines the audio characteristics and encoding optimizations.
    /// </summary>
    public OpusSignal Signal { get; set; } = OpusSignal.OpusVoice;

    /// <summary>
    /// Gets or sets the complexity level for the Opus encoder, which determines the computational resources and encoding quality.
    /// </summary>
    public byte Complexity { get; set; } = 10; // Максимальная сложность

    /// <summary>
    /// Gets or sets the force channel mode for the Opus encoder, which determines the channel configuration and encoding behavior.
    /// </summary>
    public OpusForceChannels ForceChannels { get; set; } = OpusForceChannels.OpusMono;

    /// <summary>
    /// Gets or sets the in-band Forward Error Correction (FEC) mode for the Opus encoder, which determines the error recovery strategy.
    /// </summary>
    public OpusInbandFecMode InbandFecMode { get; set; } = OpusInbandFecMode.OpusEnabledWithSilkSwitch;

    /// <summary>
    /// Gets or sets the DTX (Discontinuous Transmission) mode for the Opus encoder,
    /// which controls whether the encoder operates in low-bitrate DTX mode to reduce bandwidth usage during silence periods.
    /// </summary>
    public OpusDtxMode DtxMode { get; set; } = OpusDtxMode.OpusEnabled;

    /// <summary>
    /// Gets or sets the prediction status for the Opus encoder, which allows enabling or disabling the use of certain predictive algorithms in encoding.
    /// </summary>
    public OpusPredictionStatus Prediction { get; set; } = OpusPredictionStatus.OpusEnabled;
}

public class OpusEncoder : AsyncDisposableWithCancel, IAudioOutput
{
    private readonly IAudioOutput _input;
    private readonly bool _disposeInput;
    private readonly int _frameSize;
    private readonly IntPtr _encoder;
    private const int OpusBitrate = 16;
    private const int MaxEncodedBytes = 48_000;
    private readonly Subject<ReadOnlyMemory<byte>> _outputSubject = new();
    private readonly byte[] _outBuffer;
    private readonly Memory<byte> _outMemory;
    private readonly ChunkingSubject _chunking;
    private readonly IDisposable _sub1;
    private readonly IDisposable? _sub2;
    public AudioFormat Format => _input.Format;
    public Observable<ReadOnlyMemory<byte>> Output => _outputSubject;

    public OpusEncoder(
        IAudioOutput input,
        OpusEncoderSettings? settings = null,
        bool useArrayPool = true,
        bool disposeInput = true)
    {
        ArgumentNullException.ThrowIfNull(input);
        _input = input;
        _disposeInput = disposeInput;
        settings ??= new OpusEncoderSettings();
        _frameSize = settings.FrameSize;
        if (input.Format.SampleRate != 8000 &&
            input.Format.SampleRate != 12000 &&
            input.Format.SampleRate != 16000 &&
            input.Format.SampleRate != 24000 &&
            input.Format.SampleRate != 48000)
        {
            throw new ArgumentOutOfRangeException(nameof(input.Format.SampleRate));
        }

        if (input.Format.Channel != 1 && input.Format.Channel != 2)
        {
            throw new ArgumentOutOfRangeException(nameof(input.Format.Channel));
        }

        if (input.Format.Bits != OpusBitrate)
        {
            throw new ArgumentOutOfRangeException(nameof(input.Format.Bits));
        }

        _encoder = OpusNative.opus_encoder_create(input.Format.SampleRate, input.Format.Channel, (int)settings.Application, out var error);
        if ((Errors)error != Errors.OpusOk)
        {
            throw new Exception($"Exception occured while creating opus encoder:{(Errors)error:G}");
        }

        CheckError(OpusNative.opus_encoder_ctl(_encoder, OpusCtl.OpusSetInbandFecRequest, settings.ForwardErrorCorrection ? 1 : 0));
        CheckError(OpusNative.opus_encoder_ctl(_encoder, OpusCtl.OpusSetBitrateRequest, settings.CodecBitrate));
        CheckError(OpusNative.opus_encoder_ctl(_encoder, OpusCtl.OpusSetSignalRequest, (int)settings.Signal));
        CheckError(OpusNative.opus_encoder_ctl(_encoder, OpusCtl.OpusSetBandwidthRequest, (int)settings.Bandwidth));
        CheckError(OpusNative.opus_encoder_ctl(_encoder, OpusCtl.OpusSetComplexityRequest, settings.Complexity));
        CheckError(OpusNative.opus_encoder_ctl(_encoder, OpusCtl.OpusSetForceChannelsRequest, (int)settings.ForceChannels));
        CheckError(OpusNative.opus_encoder_ctl(_encoder, OpusCtl.OpusSetInbandFecRequest, (int)settings.InbandFecMode));
        CheckError(OpusNative.opus_encoder_ctl(_encoder, OpusCtl.OpusSetDtxRequest, (int)settings.DtxMode));
        CheckError(OpusNative.opus_encoder_ctl(_encoder, OpusCtl.OpusSetPredictionDisabledRequest, (int)settings.Prediction));

        var chunkByteSize = _frameSize * input.Format.BytesPerSample;
        _chunking = new ChunkingSubject(input, chunkByteSize, useArrayPool);
        _sub1 = _chunking.Output.Subscribe(Encode);

        // src.Chunking(chunkByteSize , useArrayPool).Subscribe(Encode).DisposeItWith(Disposable);
        if (useArrayPool)
        {
            _outBuffer = ArrayPool<byte>.Shared.Rent(MaxEncodedBytes);
            _sub2 = Disposable.Create(() => ArrayPool<byte>.Shared.Return(_outBuffer));
        }
        else
        {
            _outBuffer = new byte[MaxEncodedBytes];
        }

        _outMemory = new Memory<byte>(_outBuffer, 0, MaxEncodedBytes);
    }

    private void Encode(ReadOnlyMemory<byte> input)
    {
        if (IsDisposed)
        {
            return;
        }

        using var inputHandle = input.Pin();
        using var outputHandle = _outMemory.Pin();
        int length;
        unsafe
        {
            length = OpusNative.opus_encode(_encoder, inputHandle.Pointer, _frameSize, outputHandle.Pointer, MaxEncodedBytes);
        }

        CheckError(length);
        _outputSubject.OnNext(new ReadOnlyMemory<byte>(_outBuffer, 0, length));
    }

    private void CheckError(int result)
    {
        if (result < 0)
        {
            throw new Exception($"Encoding failed - {(Errors)result:G}" );
        }
    }

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_disposeInput)
            {
                _input.Dispose();
            }

            _outputSubject.Dispose();
            _chunking.Dispose();
            _sub1.Dispose();
            _sub2?.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_disposeInput)
        {
            await _input.DisposeAsync();
        }

        await CastAndDispose(_outputSubject);
        await _chunking.DisposeAsync();
        await CastAndDispose(_sub1);
        if (_sub2 != null)
        {
            await CastAndDispose(_sub2);
        }

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