using System.Collections.Immutable;
using DynamicData;
using DynamicData.Binding;

namespace Asv.Audio;

public interface IAudioDeviceInfo
{
    string Id { get; }
    IAudioSource Source { get; }
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
    string Id { get; }
    IObservable<IChangeSet<IAudioDeviceInfo,string>> CaptureDevices { get; }
    IAudioCaptureDevice? CreateCaptureDevice(string deviceId, AudioFormat format);
    IObservable<IChangeSet<IAudioDeviceInfo,string>> RenderDevices { get; }
    IAudioRenderDevice? CreateRenderDevice(string deviceId, AudioFormat format);
    
    public ImmutableArray<IAudioDeviceInfo> GetAllCaptureDevices()
    {
        using var s1 = CaptureDevices.BindToObservableList(out var list).Subscribe();
        var result = list.Items.ToImmutableArray();
        list.Dispose();
        return result;
    }
    
    public IAudioCaptureDevice? CreateFirstCaptureDevice(AudioFormat format)
    {
        using var s1 = CaptureDevices.BindToObservableList(out var list).Subscribe();
        var first = list.Items.FirstOrDefault();
        list.Dispose();
        return first == null ? null : CreateCaptureDevice(first.Id, format);
    }
    
    public IAudioRenderDevice? CreateFirstRenderDevice(AudioFormat format)
    {
        using var s1 = RenderDevices.BindToObservableList(out var list).Subscribe();
        var first = list.Items.FirstOrDefault();
        list.Dispose();
        return first == null ? null : CreateRenderDevice(first.Id, format);
    }
    
    public ImmutableArray<IAudioDeviceInfo> GetAllRenderDevices()
    {
        using var s1 = RenderDevices.BindToObservableList(out var list).Subscribe();
        var result = list.Items.ToImmutableArray();
        list.Dispose();
        return result;
    }
}