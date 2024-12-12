using R3;

namespace Asv.Audio;

public interface IAudioOutput : IDisposable, IAsyncDisposable
{
    AudioFormat Format { get; }
    Observable<ReadOnlyMemory<byte>> Output { get; }
}