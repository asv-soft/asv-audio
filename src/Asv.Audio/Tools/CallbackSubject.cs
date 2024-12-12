using Asv.Common;
using R3;

namespace Asv.Audio;

public class CallbackSubject : AsyncDisposableWithCancel, IAudioOutput
{
    private readonly Subject<ReadOnlyMemory<byte>> _onData = new();
    private readonly IAudioOutput _src;
    private readonly bool _disposeInput;
    private readonly IDisposable _sub1;

    public CallbackSubject(IAudioOutput src, Action<ReadOnlyMemory<byte>> callback, bool disposeInput = true)
    {
        ArgumentNullException.ThrowIfNull(src);
        _src = src;
        _disposeInput = disposeInput;
        _sub1 = _src.Output.Do(x=>callback(x)).Subscribe(_onData.AsObserver());
    }

    public AudioFormat Format => _src.Format;
    public Observable<ReadOnlyMemory<byte>> Output => _onData;

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _onData.Dispose();
            if (_disposeInput)
            {
                _src.Dispose();
            }
            
            _sub1.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await CastAndDispose(_onData);
        if (_disposeInput)
        {
            await _src.DisposeAsync();
        }

        await CastAndDispose(_sub1);

        await base.DisposeAsyncCore();

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
            {
                await resourceAsyncDisposable.DisposeAsync();
            }
            else
            {
                resource.Dispose();
            }
        }
    }

    #endregion
}