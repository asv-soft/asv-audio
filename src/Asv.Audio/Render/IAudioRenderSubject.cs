namespace Asv.Audio;

public interface IAudioRenderSubject:IObserver<ReadOnlyMemory<byte>>
{
    AudioFormat Format { get; }
}