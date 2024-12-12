using System.Buffers;
using Asv.Common;
using R3;

namespace Asv.Audio.Codec.Opus;

public class OpusDecoder : AsyncDisposableWithCancel, IAudioOutput
{
    private readonly IAudioOutput _input;
    private readonly int _frameSize;
    private readonly bool _disposeInput;
    private const int OpusBitrate = 16;
    private const int MaxDecodedSize = 48_000;
    private readonly Subject<ReadOnlyMemory<byte>> _outputSubject = new();
    private readonly byte[] _outBuffer;
    private readonly Memory<byte> _outMemory;
    private readonly IntPtr _decoder;
    private readonly IDisposable _sub1;
    private readonly IDisposable? _sub2;

    public AudioFormat Format => _input.Format;
    public Observable<ReadOnlyMemory<byte>> Output => this._outputSubject;

    public OpusDecoder(IAudioOutput input, int frameSize = OpusEncoderSettings.DefaultFrameSize,  bool useArrayPool = true, bool disposeInput = true)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frameSize);

        if (input.Format.SampleRate != 8000 &&
            input.Format.SampleRate != 12000 &&
            input.Format.SampleRate != 16000 &&
            input.Format.SampleRate != 24000 &&
            input.Format.SampleRate != 48000)
        {
            throw new ArgumentOutOfRangeException(nameof(input.Format.SampleRate));
        }

        if (input.Format.Channel != 1 && input.Format.Channel != 2)
        {
            throw new ArgumentOutOfRangeException(nameof(input.Format.Channel));
        }

        if (input.Format.Bits != OpusBitrate)
        {
            throw new ArgumentOutOfRangeException(nameof(input.Format.Bits));
        }

        _decoder = OpusNative.opus_decoder_create(input.Format.SampleRate, input.Format.Channel, out var error);
        if ((Errors)error != Errors.OpusOk)
        {
            throw new Exception($"Exception occured while creating opus decoder:{(Errors)error:G}");
        }

        _input = input;
        _frameSize = frameSize;
        _disposeInput = disposeInput;

        _sub1 = input.Output.Subscribe(Decode);

        if (useArrayPool)
        {
            _outBuffer = ArrayPool<byte>.Shared.Rent(MaxDecodedSize);
            _sub2 = Disposable.Create(() => ArrayPool<byte>.Shared.Return(_outBuffer));
        }
        else
        {
            _outBuffer = new byte[MaxDecodedSize];
        }

        _outMemory = new Memory<byte>(_outBuffer, 0, MaxDecodedSize);
    }

    private void Decode(ReadOnlyMemory<byte> input)
    {
        if (IsDisposed)
        {
            return;
        }

        using var outputHandle = _outMemory.Pin();
        int length;
        if (input.IsEmpty)
        {
            unsafe
            {
                length = OpusNative.opus_decode(_decoder, null, input.Length, outputHandle.Pointer, _frameSize, 1);    
            }
        }
        else
        {
            using var inputHandle = input.Pin();
            unsafe
            {
                length = OpusNative.opus_decode(_decoder, inputHandle.Pointer, input.Length, outputHandle.Pointer, _frameSize, 0);
            }
        }

        CheckError(length);
        _outputSubject.OnNext(new ReadOnlyMemory<byte>(_outBuffer, 0, length * 2));
    }

    private void CheckError(int result)
    {
        if (result < 0)
        {
            throw new Exception($"Decoding failed - {(Errors)result:G}" );
        }
    }

    #region Dispose

    private void ReleaseUnmanagedResources()
    {
        if (_decoder != IntPtr.Zero)
        {
            OpusNative.opus_decoder_destroy(_decoder);
        }
    }

    protected override void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources();
        if (disposing)
        {
            if (_disposeInput)
            {
                _input.Dispose();
            }

            _outputSubject.Dispose();
            _sub1.Dispose();
            _sub2?.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_disposeInput)
        {
            await _input.DisposeAsync();
        }

        await CastAndDispose(_outputSubject);
        await CastAndDispose(_sub1);
        ReleaseUnmanagedResources();

        if (_sub2 != null)
        {
            await CastAndDispose(_sub2);
        }

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

    ~OpusDecoder()
    {
        Dispose(false);
    }

    #endregion
}