namespace Asv.Audio;

public interface IAudioRenderDevice:IAudioRenderSubject
{
    void Start();
    void Stop();
}