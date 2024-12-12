using System.Buffers;
using Asv.Common;
using R3;

namespace Asv.Audio;

public class ChunkingSubject : AsyncDisposableWithCancel, IAudioOutput
{
    private readonly IAudioOutput _src;
    private readonly int _chunkByteSize;
    private readonly bool _disposeInput;
    private readonly Subject<ReadOnlyMemory<byte>> _onData = new();
    private readonly byte[] _notUsedBuffer;
    private int _notUsedBufferSize;
    private readonly IDisposable _sub1;
    private readonly IDisposable? _sub2;

    public AudioFormat Format => _src.Format;
    public Observable<ReadOnlyMemory<byte>> Output => _onData;
    
    public ChunkingSubject(IAudioOutput src, int chunkByteSize, bool useArrayPool = true, bool disposeInput = true)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chunkByteSize);
        ArgumentNullException.ThrowIfNull(src);
        _src = src;
        _chunkByteSize = chunkByteSize;
        _disposeInput = disposeInput;
        _sub1 = _src.Output.Subscribe(Process);
        
        if (useArrayPool)
        {
            _notUsedBuffer = ArrayPool<byte>.Shared.Rent(chunkByteSize);
            _sub2 = Disposable.Create(() => ArrayPool<byte>.Shared.Return(_notUsedBuffer));
        }
        else
        {
            _notUsedBuffer = new byte[chunkByteSize];
        }
    }

    private void Process(ReadOnlyMemory<byte> readOnlyMemory)
    {
        if (IsDisposed) return;
        
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
        _onData.OnNext(new ReadOnlyMemory<byte>(_notUsedBuffer,0, _chunkByteSize));
        
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

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_disposeInput) _src.Dispose();
            _onData.Dispose();
            _sub1.Dispose();
            _sub2?.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_disposeInput) await _src.DisposeAsync();
        await CastAndDispose(_onData);
        await CastAndDispose(_sub1);
        if (_sub2 != null) await CastAndDispose(_sub2);

        await base.DisposeAsyncCore();

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }

    #endregion
    
}