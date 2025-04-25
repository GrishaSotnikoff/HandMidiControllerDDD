// Program.cs
using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HandMidiControllerDDD.Application.Interfaces;
using HandMidiControllerDDD.Infrastructure;

var services = new ServiceCollection();

// 1) Console logging
services.AddLogging(cfg =>
{
    cfg.AddConsole();
    cfg.SetMinimumLevel(LogLevel.Debug);
});

// 2) Hook up your services
services.AddSingleton<IMidiService, MidiService>();
services.AddSingleton<ICameraService, CameraService>();

var provider = services.BuildServiceProvider();
var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger("Program");

logger.LogInformation("🚀 Booting HandMidiControllerDDD...");

// 3) Resolve ICameraService to kick off the WebSocket + MIDI loops
var cameraService = provider.GetRequiredService<ICameraService>();

// 4) Keep the app alive until Ctrl+C
logger.LogInformation("Press Ctrl+C to exit.");
var exitEvent = new ManualResetEvent(false);
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    exitEvent.Set();
};
exitEvent.WaitOne();

// 5) Clean shutdown
logger.LogInformation("👋 Shutting down services...");
cameraService.Dispose();
