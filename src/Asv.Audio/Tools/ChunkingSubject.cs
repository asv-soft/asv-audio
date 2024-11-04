using System.Buffers;
using System.Reactive.Subjects;
using Asv.Common;

namespace Asv.Audio;

public class ChunkingSubject<T> : DisposableOnceWithCancel, IObservable<ReadOnlyMemory<T>>
{
    private readonly int _chunkByteSize;
    private readonly Subject<ReadOnlyMemory<T>> _onData;
    private readonly T[] _notUsedBuffer;
    private int _notUsedBufferSize;

    public ChunkingSubject(
        IObservable<ReadOnlyMemory<T>> src,
        int chunkByteSize,
        bool useArrayPool = true
    )
    {
        _chunkByteSize = chunkByteSize;
        _onData = new Subject<ReadOnlyMemory<T>>().DisposeItWith(Disposable);
        Disposable.Add(src.Subscribe(Process));
        if (useArrayPool)
        {
            _notUsedBuffer = ArrayPool<T>.Shared.Rent(chunkByteSize);
            Disposable.AddAction(() => ArrayPool<T>.Shared.Return(_notUsedBuffer));
        }
        else
        {
            _notUsedBuffer = new T[chunkByteSize];
        }
    }

    private void Process(ReadOnlyMemory<T> readOnlyMemory)
    {
        if (IsDisposed)
        {
            return;
        }

        if ((readOnlyMemory.Length + _notUsedBufferSize) < _chunkByteSize)
        {
            // not enough data
            readOnlyMemory.CopyTo(_notUsedBuffer.AsMemory(_notUsedBufferSize));
            _notUsedBufferSize += readOnlyMemory.Length;
            return;
        }

        var bytesToCopy = _chunkByteSize - _notUsedBufferSize;
        readOnlyMemory[..bytesToCopy].CopyTo(_notUsedBuffer.AsMemory(_notUsedBufferSize));
        readOnlyMemory = readOnlyMemory[bytesToCopy..];
        _onData.OnNext(new ReadOnlyMemory<T>(_notUsedBuffer, 0, _chunkByteSize));

        var fullChunks = readOnlyMemory.Length / _chunkByteSize;
        for (var i = 0; i < fullChunks; i++)
        {
            _onData.OnNext(readOnlyMemory.Slice(i * _chunkByteSize, _chunkByteSize));
        }

        _notUsedBufferSize = readOnlyMemory.Length % _chunkByteSize;
        if (_notUsedBufferSize > 0)
        {
            readOnlyMemory[(fullChunks * _chunkByteSize)..].CopyTo(_notUsedBuffer.AsMemory(0));
        }
    }

    public IDisposable Subscribe(IObserver<ReadOnlyMemory<T>> observer)
    {
        return _onData.Subscribe(observer);
    }
}
