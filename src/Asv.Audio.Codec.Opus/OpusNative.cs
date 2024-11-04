using System.Runtime.InteropServices;

namespace Asv.Audio.Codec.Opus;

public static class OpusNative
{
    [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr OpusEncoderCreate(
        int fs,
        int channels,
        int application,
        out IntPtr error
    );

    [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void OpusEncoderDestroy(IntPtr encoder);

    [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
    internal static extern unsafe int OpusEncode(
        IntPtr st,
        void* pcm,
        int frameSize,
        void* data,
        int maxDataBytes
    );

    [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr OpusDecoderCreate(int fs, int channels, out IntPtr error);

    [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void OpusDecoderDestroy(IntPtr decoder);

    [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
    internal static extern unsafe int OpusDecode(
        IntPtr st,
        void* data,
        int len,
        void* pcm,
        int frameSize,
        int decodeFec
    );

    [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int OpusEncoderCtl(IntPtr st, OpusCtl request, int value);

    [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int OpusEncoderCtl(IntPtr st, OpusCtl request, out int value);
}

public enum OpusCtl : int
{
    OpusSetApplicationRequest = 4000,
    OpusGetApplicationRequest = 4001,
    OpusSetBitrateRequest = 4002,
    OpusGetBitrateRequest = 4003,
    OpusSetMaxBandwidthRequest = 4004,
    OpusGetMaxBandwidthRequest = 4005,
    OpusSetVbrRequest = 4006,
    OpusGetVbrRequest = 4007,
    OpusSetBandwidthRequest = 4008,
    OpusGetBandwidthRequest = 4009,
    OpusSetComplexityRequest = 4010,
    OpusGetComplexityRequest = 4011,
    OpusSetInbandFecRequest = 4012,
    OpusGetInbandFecRequest = 4013,
    OpusSetPacketLossPercRequest = 4014,
    OpusGetPacketLossPercRequest = 4015,
    OpusSetDtxRequest = 4016,
    OpusGetDtxRequest = 4017,
    OpusSetVbrConstraintRequest = 4020,
    OpusGetVbrConstraintRequest = 4021,
    OpusSetForceChannelsRequest = 4022,
    OpusGetForceChannelsRequest = 4023,
    OpusSetSignalRequest = 4024,
    OpusGetSignalRequest = 4025,
    OpusGetLookaheadRequest = 4027,
    OpusGetSampleRateRequest = 4029,
    OpusGetFinalRangeRequest = 4031,
    OpusGetPitchRequest = 4033,
    OpusSetGainRequest = 4034,
    OpusGetGainRequest = 4045,
    OpusSetLsbDepthRequest = 4036,
    OpusGetLsbDepthRequest = 4037,
    OpusGetLastPacketDurationRequest = 4039,
    OpusSetExpertFrameDurationRequest = 4040,
    OpusGetExpertFrameDurationRequest = 4041,
    OpusSetPredictionDisabledRequest = 4042,
    OpusGetPredictionDisabledRequest = 4043,
    OpusSetPhaseInversionDisabledRequest = 4046,
    OpusGetPhaseInversionDisabledRequest = 4047,
    OpusGetInDtxRequest = 4049,
}

public enum OpusSignal
{
    /// <summary>
    /// Auto-detect the signal type (default)
    /// </summary>
    Auto = -1000,

    /// <summary>
    /// Optimize for voice signals
    /// </summary>
    Voice = 3001,

    /// <summary>
    /// Optimize for music signals
    /// </summary>
    Music = 3002,
}

public enum OpusForceChannels
{
    /// <summary>
    /// Автоматический выбор количества каналов (по умолчанию)
    /// </summary>
    Auto = -1000, // Значение OPUS_AUTO в Opus SDK

    /// <summary>
    /// Принудительное кодирование в моно
    /// </summary>
    Mono = 1,

    /// <summary>
    /// Принудительное кодирование в стерео
    /// </summary>
    Stereo = 2,
}

public enum OpusBandwidth
{
    /// <summary>
    /// 4 kHz bandwidth (Narrowband)
    /// </summary>
    Narrowband = 1101,

    /// <summary>
    /// 6 kHz bandwidth (Mediumband)
    /// </summary>
    Mediumband = 1102,

    /// <summary>
    /// 8 kHz bandwidth (Wideband)
    /// </summary>
    Wideband = 1103,

    /// <summary>
    /// 12 kHz bandwidth (Super-Wideband)
    /// </summary>
    SuperWideband = 1104,

    /// <summary>
    /// 20 kHz bandwidth (Fullband)
    /// </summary>
    Fullband = 1105,
}

/// <summary>
/// Supported coding modes.
/// </summary>
public enum OpusApplication
{
    /// <summary>
    /// Best for most VoIP/videoconference applications where listening quality and intelligibility matter most.
    /// </summary>
    Voip = 2048,

    /// <summary>
    /// Best for broadcast/high-fidelity application where the decoded audio should be as close as possible to input.
    /// </summary>
    Audio = 2049,

    /// <summary>
    /// Only use when lowest-achievable latency is what matters most. Voice-optimized modes cannot be used.
    /// </summary>
    RestrictedLowLatency = 2051,
}

public enum Errors
{
    /// <summary>
    /// No error.
    /// </summary>
    OK = 0,

    /// <summary>
    /// One or more invalid/out of range arguments.
    /// </summary>
    BadArg = -1,

    /// <summary>
    /// The mode struct passed is invalid.
    /// </summary>
    BufferToSmall = -2,

    /// <summary>
    /// An internal error was detected.
    /// </summary>
    InternalError = -3,

    /// <summary>
    /// The compressed data passed is corrupted.
    /// </summary>
    InvalidPacket = -4,

    /// <summary>
    /// Invalid/unsupported request number.
    /// </summary>
    Unimplemented = -5,

    /// <summary>
    /// An encoder or decoder structure is invalid or already freed.
    /// </summary>
    InvalidState = -6,

    /// <summary>
    /// Memory allocation has failed.
    /// </summary>
    AllocFail = -7,
}

public enum OpusInbandFecMode
{
    /// <summary>
    /// Встраиваемая FEC отключена (по умолчанию).
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Встраиваемая FEC включена. Если уровень потерь пакетов достаточно высок, Opus автоматически переключится на режим SILK даже при высоких битрейтах для использования FEC.
    /// </summary>
    EnabledWithSilkSwitch = 1,

    /// <summary>
    /// Встраиваемая FEC включена, но не обязательно переключается на режим SILK при наличии музыки.
    /// </summary>
    EnabledWithoutSilkSwitch = 2,
}

public enum OpusDtxMode
{
    /// <summary>
    /// DTX отключен (по умолчанию).
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// DTX включен.
    /// </summary>
    Enabled = 1,
}

public enum OpusPredictionStatus
{
    /// <summary>
    /// Предсказание включено (по умолчанию).
    /// </summary>
    Enabled = 0,

    /// <summary>
    /// Предсказание отключено.
    /// </summary>
    Disabled = 1,
}
