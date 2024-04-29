using DynamicData;

namespace Asv.Audio;

public interface IAudioDeviceInfo
{
    string Id { get; }
    string Name { get; }
}

public class AudioFormat
{
    public AudioFormat(int sampleRate, int bits, int channel)
    {
        SampleRate = sampleRate;
        Bits = bits;
        Channel = channel;
        BytesPerSample = (bits / 8) * Channel;
    }

    public int SampleRate { get; }
    public int Bits { get; }
    public int Channel { get; }
    
    public int BytesPerSample { get; } 
    
    public override string ToString()
    {
        return $"{SampleRate}Hz {Bits}bit {Channel}ch";
    }
    
}
public interface IAudioSource
{
    IObservable<IChangeSet<IAudioDeviceInfo,string>> CaptureDevices { get; }
    IAudioCaptureDevice CreateCaptureDevice(string deviceId, AudioFormat format);
    IObservable<IChangeSet<IAudioDeviceInfo,string>> RenderDevices { get; }
    IAudioRenderDevice CreateRenderDevice(string deviceId, AudioFormat format);
}