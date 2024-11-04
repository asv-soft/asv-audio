using System.Reactive.Subjects;
using Asv.Common;
using NAudio.Wave;

namespace Asv.Audio.Source.Windows;

public class Mp3AudioCaptureDevice : DisposableOnceWithCancel, IAudioCaptureDevice
{
    private readonly string _fileName;
    private readonly AudioFormat _format;
    private Thread? _playThread;
    private CancellationTokenSource? _cancel;
    private readonly Subject<ReadOnlyMemory<byte>> _onData;

    public Mp3AudioCaptureDevice(string fileName, AudioFormat format)
    {
        _fileName = fileName;
        _format = format;
        _onData = new Subject<ReadOnlyMemory<byte>>().DisposeItWith(Disposable);
        Disposable.AddAction(() =>
        {
            _cancel?.Cancel(false);
        });
    }

    public IDisposable Subscribe(IObserver<ReadOnlyMemory<byte>> observer)
    {
        return _onData.Subscribe(observer);
    }

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
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var cancel = (CancellationToken)obj!;
        try
        {
            using var ms = File.OpenRead(_fileName);
            using var rdr = new Mp3FileReader(ms);
            using var wavStream = WaveFormatConversionStream.CreatePcmStream(rdr);
            using var reSampler = new MediaFoundationResampler(
                wavStream,
                new WaveFormat(_format.SampleRate, _format.Bits, _format.Channel)
            );
            var buffLen = _format.SampleRate * _format.BytesPerSample;

            while (cancel.IsCancellationRequested == false)
            {
                var data = new byte[buffLen];
                var read = reSampler.Read(data, 0, buffLen);
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
}
