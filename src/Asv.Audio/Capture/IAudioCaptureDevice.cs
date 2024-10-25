namespace Asv.Audio;

public interface IAudioCaptureDevice:IObservable<ReadOnlyMemory<byte>>, IDisposable
{
    void Start();
    void Stop();
}
