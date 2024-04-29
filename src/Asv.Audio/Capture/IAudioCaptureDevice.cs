namespace Asv.Audio;

public interface IAudioCaptureDevice:IAudioCaptureSubject
{
    void Start();
    void Stop();
}