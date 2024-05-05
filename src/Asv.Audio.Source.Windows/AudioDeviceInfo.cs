namespace Asv.Audio.Source.Windows;

internal class AudioDeviceInfo(string id, string friendlyName) : IAudioDeviceInfo
{
    public string Id { get; } = id;
    public string Name { get; } = friendlyName;
}