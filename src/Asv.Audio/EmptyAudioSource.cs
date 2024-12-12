using Asv.Common;
using DynamicData;

namespace Asv.Audio;

public class EmptyAudioSource : DisposableOnceWithCancel, IAudioSource
{
    private static volatile IAudioSource? _instance;
    private static readonly object Sync = new();

    public static IAudioSource Instance
    {
        get
        {
            if (_instance != null)
            {
                return _instance;
            }

            lock (Sync)
            {
                if (_instance != null)
                {
                    return _instance;
                }

                return _instance = new EmptyAudioSource();
            }
        }
    }

    public EmptyAudioSource(string id = "empty")
    {
        Id = id;
        CaptureDevices = new SourceCache<IAudioDeviceInfo, string>(x => x.Id)
            .DisposeItWith(Disposable)
            .Connect()
            .RefCount();
        RenderDevices = new SourceCache<IAudioDeviceInfo, string>(x => x.Id)
            .DisposeItWith(Disposable)
            .Connect()
            .RefCount();
    }

    public string Id { get; }
    public IObservable<IChangeSet<IAudioDeviceInfo, string>> CaptureDevices { get; }

    public IAudioCaptureDevice? CreateCaptureDevice(string deviceId, AudioFormat format)
    {
        throw new Exception("Empty audio source");
    }

    public IObservable<IChangeSet<IAudioDeviceInfo, string>> RenderDevices { get; }

    public IAudioRenderDevice? CreateRenderDevice(string deviceId, AudioFormat format)
    {
        throw new Exception("Empty audio source");
    }
}
