using System.Buffers;
using System.Reactive.Subjects;
using Asv.Common;

namespace Asv.Audio.Codec.Opus;

public class OpusEncoderSettings
{
    public const int DefaultFrameSize = 960 * 6; // 120 ms
    
    /// <summary>
    /// Приложение, для которого используется кодек Opus (например, VoIP).
    /// </summary>
    public OpusApplication Application { get; set; } = OpusApplication.Voip;

    /// <summary>
    /// Использование встроенной коррекции ошибок (FEC).
    /// </summary>
    public bool ForwardErrorCorrection { get; set; } = false;

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

public class OpusEncoder: DisposableOnceWithCancel, IObservable<ReadOnlyMemory<byte>>
{
    private readonly int _frameSize;
    private readonly IntPtr _encoder;
    private const int OpusBitrate = 16;
    private const int MaxEncodedBytes = 48_000;
    private readonly Subject<ReadOnlyMemory<byte>> _outputSubject;
    private readonly byte[] _outBuffer;
    private readonly Memory<byte> _outMemory;


    /// <summary>
    /// Represents an Opus encoder that encodes audio data into Opus format.
    /// </summary>
    public OpusEncoder(IObservable<ReadOnlyMemory<byte>> src,
        AudioFormat pcmFormat,OpusEncoderSettings? settings = null,
        bool useArrayPool = true)
    {
        settings ??= new OpusEncoderSettings();
        _frameSize = settings.FrameSize;
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
        
        _encoder = OpusNative.opus_encoder_create(pcmFormat.SampleRate, pcmFormat.Channel, (int)settings.Application, out var error);
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