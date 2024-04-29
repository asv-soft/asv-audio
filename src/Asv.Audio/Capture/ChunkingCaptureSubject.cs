using System.Buffers;
using System.Reactive.Subjects;
using Asv.Common;

namespace Asv.Audio;

public class ChunkingCaptureSubject(IAudioCaptureSubject src, int chunkByteSize, bool useArrayPool = true)
    : ChunkingSubject<byte>(src, chunkByteSize, useArrayPool), IAudioCaptureSubject
{
    public AudioFormat Format { get; } = src.Format;
}

