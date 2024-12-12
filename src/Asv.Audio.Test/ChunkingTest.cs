using System.Reactive.Linq;
using R3;
using Xunit;

namespace Asv.Audio.Test;

public class ChunkingTest
{
    [Fact]
    public void Simulate_capture_device_and_check_chunking()
    {
        const int size = 30;
        var chunks = new[] { size / 3, size / 3, size / 3, (size * 3) + 2, size - 2 };
        var data = new byte[chunks.Sum()];
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = (byte)i;
        }
        
        var device = new MemoryCaptureDevice(data,chunks, new AudioFormat(48000, 16, 1));
        var cnt = 0;
        device.Chunking(size).Output.Subscribe(x =>
        {
            Assert.Equal(size,x.Length);
            Assert.Equal(data[(cnt * size).. ((cnt + 1) * size)].ToArray(), x.ToArray());
            cnt++;
        });
        device.Start();
        Assert.Equal(5, cnt);
    }
}