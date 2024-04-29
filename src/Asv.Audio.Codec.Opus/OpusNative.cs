using System.Runtime.InteropServices;

namespace Asv.Audio.Codec.Opus;

public static class OpusNative
{
    
    static OpusNative()
    {
        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            CheckFile("libopus.so", Libs.opus_linux_armv7);    
        }
        else
        {
            if (Environment.Is64BitProcess)
            {
                CheckFile("opus.dll", Libs.opus_win_x32);    
            }
            else
            {
                CheckFile("opus.dll", Libs.opus_win_x32);    
            }    
        }
    }
    
    private static void CheckFile(string path, byte[] data)
    {
        if (!File.Exists(path)) File.WriteAllBytes(path, data);
    }
    /*[DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool SetDllDirectory(string path);*/
    
    [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr opus_encoder_create(int Fs, int channels, int application, out IntPtr error);

    [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void opus_encoder_destroy(IntPtr encoder);

    [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
    internal static extern unsafe int opus_encode(IntPtr st, void* pcm, int frame_size, void* data, int max_data_bytes);

    [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr opus_decoder_create(int Fs, int channels, out IntPtr error);

    [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void opus_decoder_destroy(IntPtr decoder);

    [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
    internal static extern unsafe int opus_decode(IntPtr st, void* data, int len, void* pcm, int frame_size, int decode_fec);

    [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int opus_encoder_ctl(IntPtr st, OpusCtl request, int value);

    [DllImport("opus", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int opus_encoder_ctl(IntPtr st, OpusCtl request, out int value);
    
}


public enum OpusCtl : int
    {
        OpusSetApplicationRequest         		= 4000,
        OpusGetApplicationRequest         		= 4001,
        OpusSetBitrateRequest             		= 4002,
        OpusGetBitrateRequest             		= 4003,
        OpusSetMaxBandwidthRequest       		= 4004,
        OpusGetMaxBandwidthRequest       		= 4005,
        OpusSetVbrRequest                 		= 4006,
        OpusGetVbrRequest                 		= 4007,
        OpusSetBandwidthRequest           		= 4008,
        OpusGetBandwidthRequest           		= 4009,
        OpusSetComplexityRequest          		= 4010,
        OpusGetComplexityRequest          		= 4011,
        OpusSetInbandFecRequest          		= 4012,
        OpusGetInbandFecRequest          		= 4013,
        OpusSetPacketLossPercRequest    		= 4014,
        OpusGetPacketLossPercRequest    		= 4015,
        OpusSetDtxRequest                 		= 4016,
        OpusGetDtxRequest                 		= 4017,
        OpusSetVbrConstraintRequest      		= 4020,
        OpusGetVbrConstraintRequest      		= 4021,
        OpusSetForceChannelsRequest      		= 4022,
        OpusGetForceChannelsRequest      		= 4023,
        OpusSetSignalRequest              		= 4024,
        OpusGetSignalRequest              		= 4025,
        OpusGetLookaheadRequest           		= 4027,
        OpusGetSampleRateRequest         		= 4029,
        OpusGetFinalRangeRequest         		= 4031,
        OpusGetPitchRequest               		= 4033,
        OpusSetGainRequest                		= 4034,
        OpusGetGainRequest                		= 4045,
        OpusSetLsbDepthRequest           		= 4036,
        OpusGetLsbDepthRequest           		= 4037,
        OpusGetLastPacketDurationRequest 		= 4039,
        OpusSetExpertFrameDurationRequest 		= 4040,
        OpusGetExpertFrameDurationRequest 		= 4041,
        OpusSetPredictionDisabledRequest 		= 4042,
        OpusGetPredictionDisabledRequest 		= 4043,
        OpusSetPhaseInversionDisabledRequest 	= 4046,
        OpusGetPhaseInversionDisabledRequest 	= 4047,
        OpusGetInDtxRequest              		= 4049,
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
        Restricted_LowLatency = 2051
    }

    public enum Errors
    {
        /// <summary>
        /// No error.
        /// </summary>
        OK              = 0,
        /// <summary>
        /// One or more invalid/out of range arguments.
        /// </summary>
        BadArg          = -1,
        /// <summary>
        /// The mode struct passed is invalid.
        /// </summary>
        BufferToSmall   = -2,
        /// <summary>
        /// An internal error was detected.
        /// </summary>
        InternalError   = -3,
        /// <summary>
        /// The compressed data passed is corrupted.
        /// </summary>
        InvalidPacket   = -4,
        /// <summary>
        /// Invalid/unsupported request number.
        /// </summary>
        Unimplemented   = -5,
        /// <summary>
        /// An encoder or decoder structure is invalid or already freed.
        /// </summary>
        InvalidState    = -6,
        /// <summary>
        /// Memory allocation has failed.
        /// </summary>
        AllocFail       = -7
    }