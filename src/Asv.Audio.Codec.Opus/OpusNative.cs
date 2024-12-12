using System.Runtime.InteropServices;

namespace Asv.Audio.Codec.Opus;

public static class OpusNative
{
    static OpusNative()
    {
        OpusHelper.CheckLibs();
    }

    [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
#pragma warning disable SA1300
    internal static extern IntPtr opus_encoder_create(int fs, int channels, int application, out IntPtr error);

    [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void opus_encoder_destroy(IntPtr encoder);

    [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
    internal static extern unsafe int opus_encode(IntPtr st, void* pcm, int frameSize, void* data, int maxDataBytes);

    [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr opus_decoder_create(int fs, int channels, out IntPtr error);

    [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void opus_decoder_destroy(IntPtr decoder);

    [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
    internal static extern unsafe int opus_decode(IntPtr st, void* data, int len, void* pcm, int frameSize, int decodeFec);

    [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int opus_encoder_ctl(IntPtr st, OpusCtl request, int value);

    [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int opus_encoder_ctl(IntPtr st, OpusCtl request, out int value);
#pragma warning restore SA1300
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
    OpusAuto = -1000,

    /// <summary>
    /// Optimize for voice signals
    /// </summary>
    OpusVoice = 3001,

    /// <summary>
    /// Optimize for music signals
    /// </summary>
    OpusMusic = 3002,
}

public enum OpusForceChannels
{
    /// <summary>
    /// Автоматический выбор количества каналов (по умолчанию)
    /// </summary>
    OpusAuto = -1000,  // Значение OPUS_AUTO в Opus SDK

    /// <summary>
    /// Принудительное кодирование в моно
    /// </summary>
    OpusMono = 1,

    /// <summary>
    /// Принудительное кодирование в стерео
    /// </summary>
    OpusStereo = 2,
}

public enum OpusBandwidth
{
    /// <summary>
    /// 4 kHz bandwidth (Narrowband)
    /// </summary>
    OpusNarrowband = 1101,

    /// <summary>
    /// 6 kHz bandwidth (Mediumband)
    /// </summary>
    OpusMediumband = 1102,

    /// <summary>
    /// 8 kHz bandwidth (Wideband)
    /// </summary>
    OpusWideband = 1103,

    /// <summary>
    /// 12 kHz bandwidth (Super-Wideband)
    /// </summary>
    OpusSuperWideband = 1104,

    /// <summary>
    /// 20 kHz bandwidth (Fullband)
    /// </summary>
    OpusFullband = 1105,
}

    /// <summary>
    /// Supported coding modes.
    /// </summary>
    public enum OpusApplication
    {
        /// <summary>
        /// Best for most VoIP/videoconference applications where listening quality and intelligibility matter most.
        /// </summary>
        OpusVoip = 2048,
        
        /// <summary>
        /// Best for broadcast/high-fidelity application where the decoded audio should be as close as possible to input.
        /// </summary>
        OpusAudio = 2049,
        
        /// <summary>
        /// Only use when lowest-achievable latency is what matters most. Voice-optimized modes cannot be used.
        /// </summary>
        OpusRestrictedLowLatency = 2051,
    }

    public enum Errors
    {
        /// <summary>
        /// No error.
        /// </summary>
        OpusOk = 0,

        /// <summary>
        /// One or more invalid/out of range arguments.
        /// </summary>
        OpusBadArg = -1,

        /// <summary>
        /// The mode struct passed is invalid.
        /// </summary>
        OpusBufferToSmall = -2,

        /// <summary>
        /// An internal error was detected.
        /// </summary>
        OpusInternalError = -3,

        /// <summary>
        /// The compressed data passed is corrupted.
        /// </summary>
        OpusInvalidPacket = -4,

        /// <summary>
        /// Invalid/unsupported request number.
        /// </summary>
        OpusUnimplemented = -5,

        /// <summary>
        /// An encoder or decoder structure is invalid or already freed.
        /// </summary>
        OpusInvalidState = -6,

        /// <summary>
        /// Memory allocation has failed.
        /// </summary>
        OpusAllocFail = -7,
    }

    public enum OpusInbandFecMode
    {
        /// <summary>
        /// Встраиваемая FEC отключена (по умолчанию).
        /// </summary>
        OpusDisabled = 0,

        /// <summary>
        /// Встраиваемая FEC включена. Если уровень потерь пакетов достаточно высок, Opus автоматически переключится на режим SILK даже при высоких битрейтах для использования FEC.
        /// </summary>
        OpusEnabledWithSilkSwitch = 1,

        /// <summary>
        /// Встраиваемая FEC включена, но не обязательно переключается на режим SILK при наличии музыки.
        /// </summary>
        OpusEnabledWithoutSilkSwitch = 2,
    }

    public enum OpusDtxMode
    {
        /// <summary>
        /// DTX отключен (по умолчанию).
        /// </summary>
        OpusDisabled = 0,

        /// <summary>
        /// DTX включен.
        /// </summary>
        OpusEnabled = 1,
    }

    public enum OpusPredictionStatus
    {
        /// <summary>
        /// Предсказание включено (по умолчанию).
        /// </summary>
        OpusEnabled = 0,

        /// <summary>
        /// Предсказание отключено.
        /// </summary>
        OpusDisabled = 1,
    }