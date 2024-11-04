namespace Asv.Audio;

public interface IAudioRenderDevice : IObserver<ReadOnlyMemory<byte>>, IDisposable
{
    void Start();
    void Stop();
}
