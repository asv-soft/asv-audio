﻿using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Linq;
using Asv.Audio.Codec.Opus;
using Asv.Audio.Source.Windows;
using Asv.Common;
using DynamicData.Binding;
using NLog;
using R3;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Asv.Audio.Shell;

internal class WindowsLoopCommand : Command<WindowsLoopCommand.Settings>
{
    public const string Name = "win-loop";
    public sealed class Settings : CommandSettings;

    public WindowsLoopCommand()
    {
        // format for raw audio
        var format = new AudioFormat(48000, 16, 1);

        // audio source for windows
        IAudioSource src = new MmWindowsAudioSource();

        // get first capture and render devices
        using var rec = src.CreateFirstCaptureDevice(format) 
                  ?? throw new Exception("Capture device not found");
        using var play = src.CreateFirstRenderDevice(format) 
                         ?? throw new Exception("Render device not found");;
        using var a = rec.OpusEncode()
            .OpusDecode()
            .Play(play);
    }

    public override int Execute(CommandContext? context, Settings settings)
    {
        var waitForProcessShutdownStart = new ManualResetEventSlim();
        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            // We got a SIGTERM, signal that graceful shutdown has started
            waitForProcessShutdownStart.Set();
        };
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            AnsiConsole.Write($"Cancel key pressed=> shutdown");
            waitForProcessShutdownStart.Set();
        };

        var src = new MmWindowsAudioSource();
        using var sub = src.CaptureDevices.BindToObservableList(out var capture).Subscribe();
        using var sub2 = src.RenderDevices.BindToObservableList(out var render).Subscribe();

        var renderSelected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select [green]render[/] device?")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more devices)[/]")
                .AddChoices(render.Items.Select(x => x.Name)));
        var renderId = render.Items.First(x => x.Name == renderSelected).Id;

        var captureSelected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select [red]capture[/] device?")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more devices)[/]")
                .AddChoices(capture.Items.Select(x => x.Name)));
        var captureId = capture.Items.First(x => x.Name == captureSelected).Id;

        var format = new AudioFormat(48000, 16, 1);

        using var renderDevice = src.CreateRenderDevice(renderId, format);
        using var captureDevice = src.CreateCaptureDevice(captureId, format);
        long rxCnt = 0;
        long rxOpus = 0;
        long framesCount = 1;

        Debug.Assert(captureDevice != null, nameof(captureDevice) + " != null");
        Debug.Assert(renderDevice != null, nameof(renderDevice) + " != null");

        using var loopSub = captureDevice
            .Do(x => rxCnt += x.Length)
            .OpusEncode()
            .Do(x =>
            {
                rxOpus += x.Length;
                ++framesCount;
            })
            .OpusDecode()
            .Play(renderDevice);
        renderDevice.Start();
        captureDevice.Start();

        var rxCounter = new IncrementalRateCounter();
        var rxOpusCounter = new IncrementalRateCounter();

        while (true)
        {
            var chart = new BarChart()
                .Width(60)
                .Label("[green bold underline]Rates[/]")
                .CenterLabel()
                .AddItem("RAW SIZE", Math.Ceiling(rxCounter.Calculate(rxCnt)), Color.Yellow)
                .AddItem("AFTER OPUS", Math.Ceiling(rxOpusCounter.Calculate(rxOpus)), Color.Green)
                .AddItem("AVG MSG SIZE", Math.Ceiling((double)(rxOpus / framesCount)), Color.Green);

            AnsiConsole.Write(chart);
            Task.Delay(1000).Wait();
        }

        return 0;
    }
}