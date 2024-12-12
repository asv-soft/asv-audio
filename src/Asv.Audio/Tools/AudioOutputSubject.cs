using Asv.Common;
using R3;

namespace Asv.Audio;

public class AudioOutputSubject(AudioFormat format, Observable<ReadOnlyMemory<byte>> output)
    : AsyncDisposableWithCancel, IAudioOutput
{
    public AudioFormat Format { get; } = format;
    public Observable<ReadOnlyMemory<byte>> Output { get; } = output;
}