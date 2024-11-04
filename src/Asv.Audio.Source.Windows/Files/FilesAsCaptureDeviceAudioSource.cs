using Asv.Common;
using DynamicData;

namespace Asv.Audio.Source.Windows;

public class FilesAsCaptureDeviceAudioSource : DisposableOnceWithCancel, IAudioSource
{
    private readonly string _audioFilesPath;
    private readonly SourceCache<IAudioDeviceInfo, string> _files;

    public FilesAsCaptureDeviceAudioSource(string id, string audioFilesPath)
    {
        _audioFilesPath = audioFilesPath;
        Id = id;
        if (!Directory.Exists(audioFilesPath))
        {
            Directory.CreateDirectory(audioFilesPath);
        }

        _files = new SourceCache<IAudioDeviceInfo, string>(x => x.Id);
        CaptureDevices = _files.DisposeItWith(Disposable).Connect().RefCount();
        RenderDevices = new SourceCache<IAudioDeviceInfo, string>(x => x.Id)
            .DisposeItWith(Disposable)
            .Connect()
            .RefCount();
        Refresh();
        var watcher = new FileSystemWatcher(audioFilesPath, "*.mp3").DisposeItWith(Disposable);
        watcher.Created += (sender, args) => Refresh();
    }

    private void Refresh()
    {
        _files.Clear();
        Directory
            .EnumerateFiles(_audioFilesPath, "*.mp3", SearchOption.AllDirectories)
            .Select(x => new AudioDeviceInfo(x, Path.GetFileName(x), this))
            .ForEach(x => _files.AddOrUpdate(x));
    }

    public string Id { get; }
    public IObservable<IChangeSet<IAudioDeviceInfo, string>> CaptureDevices { get; }

    public IAudioCaptureDevice? CreateCaptureDevice(string deviceId, AudioFormat format)
    {
        return new Mp3AudioCaptureDevice(deviceId, format);
    }

    public IObservable<IChangeSet<IAudioDeviceInfo, string>> RenderDevices { get; }

    public IAudioRenderDevice? CreateRenderDevice(string deviceId, AudioFormat format)
    {
        return null;
    }
}
