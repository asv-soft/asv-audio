namespace Asv.Audio.Codec.Opus;

public static class OpusHelper
{
    public static IObservable<ReadOnlyMemory<byte>> OpusEncode(this IObservable<ReadOnlyMemory<byte>> input, AudioFormat pcmFormat, OpusApplication app = OpusApplication.Voip, bool forwardErrorCorrection = false, int segmentFrames = 960, int codecBitrate = 8000, bool useArrayPool = true)
    {
        return new OpusEncoder(input, pcmFormat, app, forwardErrorCorrection, segmentFrames, codecBitrate, useArrayPool);
    }
    
    public static IObservable<ReadOnlyMemory<byte>> OpusDecode(this IObservable<ReadOnlyMemory<byte>> input, AudioFormat pcmFormat, OpusApplication app = OpusApplication.Voip, bool forwardErrorCorrection = false, int segmentFrames = 960, int codecBitrate = 8000, bool useArrayPool = true)
    {
        return new OpusDecoder(input, pcmFormat, forwardErrorCorrection, useArrayPool);
    }

    public static void CheckLibs()
    {
        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            CheckFile("libopus.so", Libs.opus_linux_armv7); // TODO: need to check processor architecture
        }
        else
        {
            if (Environment.Is64BitProcess)
            {
                CheckFile("opus.dll", Libs.opus_win_x64);
            }
            else
            {
                CheckFile("opus.dll", Libs.opus_win_x32);
            }
        }
    }

    private static void CheckFile(string path, byte[] data)
    {
#if DEBUG
        File.Delete(path);
#endif        
        if (!File.Exists(path)) File.WriteAllBytes(path, data);
    }
  
}