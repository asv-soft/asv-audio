using System.Buffers;
using Asv.Common;
using R3;

namespace Asv.Audio.Codec.Opus;

public class OpusEncoderSettings
{
    public const int DefaultFrameSize = 960*6; // 120 ms
    
    /// <summary>
    /// Приложение, для которого используется кодек Opus (например, VoIP).
    /// </summary>
    public OpusApplication Application { get; set; } = OpusApplication.Voip;

    /// <summary>
    /// Использование встроенной коррекции ошибок (FEC).
    /// </summary>
    public bool ForwardErrorCorrection { get; set; } = true;

    /// <summary>
    /// Размер кадра (в сэмплах) для кодирования.
    /// </summary>
    public int FrameSize { get; set; } = DefaultFrameSize;

    /// <summary>
    /// Битрейт кодека Opus (в битах в секунду).
    /// </summary>
    public int CodecBitrate { get; set; } = 6000;

    /// <summary>
    /// Пропускная способность кодека (ширина полосы).
    /// </summary>
    public OpusBandwidth Bandwidth { get; set; } = OpusBandwidth.Narrowband;

    /// <summary>
    /// Тип сигнала (голос или музыка).
    /// </summary>
    public OpusSignal Signal { get; set; } = OpusSignal.Voice;

    /// <summary>
    /// Сложность кодирования (от 0 до 10).
    /// </summary>
    public byte Complexity { get; set; } = 10; // Максимальная сложность

    /// <summary>
    /// Принудительное использование моно или стерео каналов.
    /// </summary>
    public OpusForceChannels ForceChannels { get; set; } = OpusForceChannels.Mono;

    /// <summary>
    /// Режим использования встроенной коррекции ошибок (FEC).
    /// </summary>
    public OpusInbandFecMode InbandFecMode { get; set; } = OpusInbandFecMode.EnabledWithSilkSwitch;

    /// <summary>
    /// Использование режима прерывистой передачи (DTX).
    /// </summary>
    public OpusDtxMode DtxMode { get; set; } = OpusDtxMode.Enabled;

    /// <summary>
    /// Статус использования предсказания в кодеке.
    /// </summary>
    public OpusPredictionStatus Prediction { get; set; } = OpusPredictionStatus.Enabled;
}

public class OpusEncoder: AsyncDisposableWithCancel, IAudioOutput
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
    
    /// <summary>
    /// Represents an Opus encoder that encodes audio data into Opus format.
    /// </summary>
    public OpusEncoder(IAudioOutput input, OpusEncoderSettings? settings = null,
        bool useArrayPool = true, bool disposeInput = true)
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
            throw new ArgumentOutOfRangeException(nameof(input.Format.SampleRate));
        if (input.Format.Channel != 1 && input.Format.Channel != 2)
            throw new ArgumentOutOfRangeException(nameof(input.Format.Channel));
        if (input.Format.Bits != 16)
            throw new ArgumentOutOfRangeException(nameof(input.Format.Bits)); // TODO: check for 8 bits
        
        _encoder = OpusNative.opus_encoder_create(input.Format.SampleRate, input.Format.Channel, (int)settings.Application, out var error);
        if ((Errors)error != Errors.OK)
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

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_disposeInput) _input.Dispose();
            _outputSubject.Dispose();
            _chunking.Dispose();
            _sub1.Dispose();
            _sub2?.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_disposeInput) await _input.DisposeAsync();
        await CastAndDispose(_outputSubject);
        await _chunking.DisposeAsync();
        await CastAndDispose(_sub1);
        if (_sub2 != null) await CastAndDispose(_sub2);

        await base.DisposeAsyncCore();

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }

    #endregion
}