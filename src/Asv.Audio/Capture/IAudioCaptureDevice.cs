namespace Asv.Audio;

public interface IAudioCaptureDevice:IObservable<ReadOnlyMemory<byte>>
{
    void Start();
    void Stop();
}