# asv-audio
Simple library for working with audio sources in .NET with a reactive interface

```csharp
// format for raw audio
var format = new AudioFormat(48000,16,1);

// platform audio source for windows (it will be extended for Linux (ALSA), Android, iOS... in future)
IAudioSource src = new MmWindowsAudioSource();

// get first capture and render devices
using var rec = src.CreateFirstCaptureDevice(format) 
          ?? throw new Exception("Capture device not found");

using var play = src.CreateFirstRenderDevice(format) 
                 ?? throw new Exception("Render device not found");

// Example 1: send audio from capture to render (loopback)
rec.Subscribe(play);

// Example 2: encode/decode audio by opus codec
rec.OpusEncode(format)
    // may be some processing here (e.g. send over network)
    .OpusDecode(format)
    .Subscribe(play);

```
