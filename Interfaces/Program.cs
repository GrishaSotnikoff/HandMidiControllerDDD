using System;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using HandMidiControllerDDD.Application.Commands;
using HandMidiControllerDDD.Application.Interfaces;
using HandMidiControllerDDD.Domain.Services;
using HandMidiControllerDDD.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Threading;
using Sanford.Multimedia.Midi;

var services = new ServiceCollection();
services.AddLogging(cfg =>
{
    cfg.AddConsole();
    cfg.SetMinimumLevel(LogLevel.Information);
});
services.AddMediatR(typeof(TrackHandPositionCommand));
services.AddSingleton<IHandTrackingService, HandTrackingService>();
services.AddSingleton<ICameraService, CameraService>();
services.AddSingleton<IMidiService, MidiService>();


var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILogger<Program>>();
var mediator = provider.GetRequiredService<IMediator>();
var camera = provider.GetRequiredService<ICameraService>();
var tracker = provider.GetRequiredService<IHandTrackingService>();
var handTracker = provider.GetRequiredService<IHandTrackingService>();

Console.WriteLine("Starting Hand MIDI Controller with DDD...");
int deviceCount = OutputDevice.DeviceCount;
Console.WriteLine($"🎛 Found {deviceCount} MIDI Output Devices:\n");

for (int i = 0; i < deviceCount; i++)
{
    try
    {
        var caps = OutputDevice.GetDeviceCapabilities(i);

        Console.WriteLine($"[{i}] {caps.name}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[{i}] Error getting device info: {ex.Message}");
    }
}
while (true)
{
    try
    {
        // grab raw position
        var pos = camera.GetHandPosition();

        // domain logic
        var gesture = tracker.TrackHand(pos.X, pos.Y, pos.Z);

        // fire off MIDI
        mediator.Send(new TrackHandPositionCommand(pos));

    }
    catch (Exception ex)
    {
        logger.LogError(ex, "🔥 Oops, something broke in the main loop");
    }
}