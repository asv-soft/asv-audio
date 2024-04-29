namespace Asv.Audio;

public static class AudioHelper
{
    public static IAudioCaptureSubject Chunking(this IAudioCaptureSubject src, int chunkByteSize,bool useArrayPool = true)
    {
        return new ChunkingCaptureSubject(src,chunkByteSize,useArrayPool);
    }
    
    
}