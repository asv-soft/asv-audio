using Asv.Common;
using R3;

namespace Asv.Audio;

public class MemoryCaptureDevice(ReadOnlyMemory<byte> data, int[] chunkSizes, AudioFormat format)
    : AsyncDisposableWithCancel, IAudioCaptureDevice
{
    private readonly Subject<ReadOnlyMemory<byte>> _dataSubject = new();

    public AudioFormat Format { get; } = format;
    public Observable<ReadOnlyMemory<byte>> Output => _dataSubject;

    public void Start()
    {
        var original = data;
        foreach (var chunk in chunkSizes)
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

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dataSubject.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        _dataSubject.Dispose();

        await base.DisposeAsyncCore();
    }

    #endregion

}