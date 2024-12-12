namespace Asv.Audio.Codec.Opus;

 public static class OpusHelper
 {
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
     
     public static IAudioOutput OpusEncode(this IAudioOutput input, OpusEncoderSettings? settings = null, bool useArrayPool = true, bool disposeInput = true)
     {
         return new OpusEncoder(input, settings, useArrayPool, disposeInput);
     }
    
     
     public static IAudioOutput OpusDecode(this IAudioOutput input, bool useArrayPool = true, bool disposeInput = true)
     {
         return new OpusDecoder(input, useArrayPool:useArrayPool, disposeInput:disposeInput);
     }
   
 }