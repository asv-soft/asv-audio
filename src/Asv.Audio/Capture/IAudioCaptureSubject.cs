namespace Asv.Audio;

public interface IAudioCaptureSubject:IObservable<ReadOnlyMemory<byte>>
{
    AudioFormat Format { get; }
}