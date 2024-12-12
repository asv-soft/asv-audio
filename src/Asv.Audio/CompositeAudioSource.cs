using System.Reactive.Linq;
using DynamicData;

namespace Asv.Audio;

public class CompositeAudioSource : IAudioSource
{
    private readonly IAudioSource[] _sources;
    internal const char Delimiter = '|';

    public CompositeAudioSource(string id, params IAudioSource[] sources)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(id));
        }

        _sources = sources;
        Id = id;
        CaptureDevices = sources
            .Select(x => x.CaptureDevices)
            .Merge()
            .Transform(x => (IAudioDeviceInfo)new CompositeDeviceInfo(x));
        RenderDevices = sources
            .Select(x => x.RenderDevices)
            .Merge()
            .Transform(x => (IAudioDeviceInfo)new CompositeDeviceInfo(x));
    }

    public string Id { get; }
    public IObservable<IChangeSet<IAudioDeviceInfo, string>> CaptureDevices { get; }

    public IAudioCaptureDevice? CreateCaptureDevice(string deviceId, AudioFormat format)
    {
        var id = ParseCompositeId(deviceId, out var sourceId);
        var source = _sources.FirstOrDefault(x => x.Id == sourceId);
        return source?.CreateCaptureDevice(id, format);
    }

    public IObservable<IChangeSet<IAudioDeviceInfo, string>> RenderDevices { get; }

    public IAudioRenderDevice? CreateRenderDevice(string deviceId, AudioFormat format)
    {
        var id = ParseCompositeId(deviceId, out var sourceId);
        var source = _sources.FirstOrDefault(x => x.Id == sourceId);
        return source?.CreateRenderDevice(id, format);
    }

    private static string ParseCompositeId(string id, out string sourceId)
    {
        var parts = id.Split(Delimiter);
        sourceId = parts[0];
        return parts[1];
    }
}

public class CompositeDeviceInfo(IAudioDeviceInfo audioDeviceInfo) : IAudioDeviceInfo
{
    public string Id { get; } =
        string.Join(CompositeAudioSource.Delimiter, audioDeviceInfo.Source.Id, audioDeviceInfo.Id);
    public IAudioSource Source { get; } = audioDeviceInfo.Source;
    public string Name { get; } = audioDeviceInfo.Name;
}
