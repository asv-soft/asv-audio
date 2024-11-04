namespace Asv.Audio.Source.Windows;

internal class AudioDeviceInfo(string id, string friendlyName, IAudioSource source)
    : IAudioDeviceInfo
{
    public string Id { get; } = id;
    public IAudioSource Source { get; } = source;
    public string Name { get; } = friendlyName;
}
