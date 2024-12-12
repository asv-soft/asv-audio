using R3;

namespace Asv.Audio;

public interface IAudioRenderDevice: IAudioInput
{
    void Start();
    void Stop();
}