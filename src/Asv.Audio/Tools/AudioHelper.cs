namespace Asv.Audio;

public static class AudioHelper
{
    public static IAudioOutput Chunking(this IAudioOutput src, int chunkByteSize,bool useArrayPool = true, bool disposeInput = true)
    {
        return new ChunkingSubject(src, chunkByteSize, useArrayPool,disposeInput);
    }
    
    public static IDisposable Play(this IAudioOutput src, IAudioInput dst)
    {
        return src.Output.Subscribe(dst.Input);
    }

    public static IAudioOutput Do(this IAudioOutput src, Action<ReadOnlyMemory<byte>> action, bool disposeInput = true)
    {
        return new CallbackSubject(src,action, disposeInput);
    }
    
}