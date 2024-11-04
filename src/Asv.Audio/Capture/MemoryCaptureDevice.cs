using System.Reactive.Subjects;
using Asv.Common;

namespace Asv.Audio;

public class MemoryCaptureDevice : DisposableOnceWithCancel, IAudioCaptureDevice
{
    private readonly ReadOnlyMemory<byte> _data;
    private readonly int[] _chunkSizes;
    private readonly Subject<ReadOnlyMemory<byte>> _dataSubject;

    public MemoryCaptureDevice(ReadOnlyMemory<byte> data, int[] chunkSizes, AudioFormat format)
    {
        _data = data;
        _chunkSizes = chunkSizes;
        Format = format;
        _dataSubject = new Subject<ReadOnlyMemory<byte>>().DisposeItWith(Disposable);
    }

    public AudioFormat Format { get; }

    public void Start()
    {
        var original = _data;
        foreach (var chunk in _chunkSizes)
        {
            if (original.Length < chunk)
            {
                throw new Exception("Not enough data");
            }

            _dataSubject.OnNext(original[..chunk]);
            original = original[chunk..];
        }
    }

    public void Stop()
    {
        _dataSubject.OnCompleted();
    }

    public IDisposable Subscribe(IObserver<ReadOnlyMemory<byte>> observer)
    {
        return _dataSubject.Subscribe(observer);
    }
}
