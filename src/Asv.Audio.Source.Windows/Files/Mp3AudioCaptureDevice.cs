using System.Diagnostics;
using Asv.Common;
using NAudio.Wave;
using R3;

namespace Asv.Audio.Source.Windows;

public class Mp3AudioCaptureDevice(string fileName, AudioFormat format) : AsyncDisposableWithCancel, IAudioCaptureDevice
{
    private Thread? _playThread;
    private CancellationTokenSource? _cancel;
    private readonly Subject<ReadOnlyMemory<byte>> _onData = new();

    public AudioFormat Format => format;
    public Observable<ReadOnlyMemory<byte>> Output => _onData;

    public void Start()
    {
        _cancel?.Cancel(false);
        _cancel?.Dispose();
        _cancel = new CancellationTokenSource();
        _playThread = new Thread(PlayLoop) { IsBackground = true };
        _playThread.Start(_cancel.Token);
    }

    private void PlayLoop(object? obj)
    {
        Debug.Assert(obj != null, "obj != null");

        // ReSharper disable once NullableWarningSuppressionIsUsed
        var cancel = (CancellationToken)obj!;
        try
        {
            using var ms = File.OpenRead(fileName);
            using var rdr = new Mp3FileReader(ms);
            using var wavStream = WaveFormatConversionStream.CreatePcmStream(rdr);
            using var resampler = new MediaFoundationResampler(wavStream, new WaveFormat(format.SampleRate, format.Bits, format.Channel));
            var buffLen = format.SampleRate * format.BytesPerSample;
            while (cancel.IsCancellationRequested == false)
            {
                var data = new byte[buffLen];
                var read = resampler.Read(data, 0, buffLen);
                if (read == 0)
                {
                    break;
                }
                
                _onData.OnNext(new ReadOnlyMemory<byte>(data, 0, read));
                Task.Delay(TimeSpan.FromSeconds(1), cancel).Wait(cancel);
            }
        }
        catch
        {
            // ignored            
        }
    }

    public void Stop()
    {
        _cancel?.Cancel(false);
        _cancel?.Dispose();
        _cancel = null;
    }

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancel?.Dispose();
            _onData.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_cancel != null)
        {
            await CastAndDispose(_cancel);
        }

        await CastAndDispose(_onData);
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