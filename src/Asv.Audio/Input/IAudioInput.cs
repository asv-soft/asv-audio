using R3;

namespace Asv.Audio;

public interface IAudioInput : IDisposable, IAsyncDisposable
{
    AudioFormat Format { get; }
    Observer<ReadOnlyMemory<byte>> Input { get; }
}