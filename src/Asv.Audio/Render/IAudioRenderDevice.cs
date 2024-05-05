namespace Asv.Audio;

public interface IAudioRenderDevice:IObserver<ReadOnlyMemory<byte>>
{
    void Start();
    void Stop();
}