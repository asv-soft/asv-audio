using System.Buffers;
using System.Reactive.Subjects;
using Asv.Common;

namespace Asv.Audio;

public class ChunkingSubject<T> : DisposableOnceWithCancel, IObservable<ReadOnlyMemory<T>>
{
    private readonly Subject<ReadOnlyMemory<T>> _onData;
    private readonly T[] _notUsedBuffer;
    private int _notUsedBufferSize;

    public ChunkingSubject(IObservable<ReadOnlyMemory<T>> src, int chunkByteSize, bool useArrayPool = true)
    {
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
        if (IsDisposed) return;
        
        if ((readOnlyMemory.Length + _notUsedBufferSize) < _notUsedBuffer.Length)
        {
            // not enough data
            readOnlyMemory.CopyTo(_notUsedBuffer.AsMemory(_notUsedBufferSize));
            return;
        }
        
        var bytesToCopy = _notUsedBuffer.Length - _notUsedBufferSize;
        readOnlyMemory[0..bytesToCopy].CopyTo(_notUsedBuffer.AsMemory(_notUsedBufferSize));
        readOnlyMemory = readOnlyMemory[bytesToCopy..];
        _onData.OnNext(new ReadOnlyMemory<T>(_notUsedBuffer));
        
        var fullChunks = readOnlyMemory.Length / _notUsedBuffer.Length;
        for (var i = 0; i < fullChunks; i++)
        {
            _onData.OnNext(readOnlyMemory.Slice(i * _notUsedBuffer.Length, _notUsedBuffer.Length));
        }

        _notUsedBufferSize = readOnlyMemory.Length % _notUsedBuffer.Length;
        if (_notUsedBufferSize > 0)
        {
            readOnlyMemory[(fullChunks * _notUsedBuffer.Length)..].CopyTo(_notUsedBuffer.AsMemory(0));
        }
    }
    
    public IDisposable Subscribe(IObserver<ReadOnlyMemory<T>> observer)
    {
        return _onData.Subscribe(observer);
    }
}

