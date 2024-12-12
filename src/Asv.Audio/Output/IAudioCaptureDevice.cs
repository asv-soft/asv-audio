using R3;

namespace Asv.Audio;

public interface IAudioCaptureDevice: IAudioOutput
{
    void Start();
    void Stop();
}
