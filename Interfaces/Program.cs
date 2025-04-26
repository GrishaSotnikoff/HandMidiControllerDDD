// Program.cs
using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HandMidiControllerDDD.Application.Interfaces;
using HandMidiControllerDDD.Infrastructure;
using Microsoft.Extensions.Options;
using Prometheus;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// 1️⃣ load your custom JSON
builder.Configuration
       .AddJsonFile("application.json", optional: false, reloadOnChange: true);

// 2️⃣ bind to POCOs

// 3️⃣ register PushGateway for pushing metrics


// … register your CameraService, MidiService, etc.
builder.Services.AddControllers();
var app = builder.Build();



var services = new ServiceCollection();

// 1) Console logging
services.AddLogging(cfg =>
{
    cfg.AddConsole();
    cfg.SetMinimumLevel(LogLevel.Debug);
});
services.Configure<HandMidiControllerDDD.Infrastructure.WebSocketOptions>(
    builder.Configuration.GetSection("WebSocketServer"));
services.Configure<HandMidiControllerDDD.Infrastructure.PrometheusOptions>(
    builder.Configuration.GetSection("Prometheus"));
services.Configure<HandMidiControllerDDD.Infrastructure.MidiOptions>(
    builder.Configuration.GetSection("Midi"));
// 2) Hook up your services
services.AddSingleton<IMidiService, MidiService>();
services.AddSingleton(sp => {
    var opts = sp.GetRequiredService<IOptions<PrometheusOptions>>().Value;
    return new MetricPusher(opts.PushGatewayUrl, opts.JobName);
});
services.AddSingleton<ICameraService, CameraService>();
var provider = services.BuildServiceProvider();
var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger("Program");

logger.LogInformation("🚀 Booting HandMidiControllerDDD...");

// 3) Resolve ICameraService to kick off the WebSocket + MIDI loops
var cameraService = provider.GetRequiredService<ICameraService>();
// 4️⃣ expose /metrics for Prometheus scrape
app.UseMetricServer();
app.UseHttpMetrics();

app.MapControllers();
app.Run();
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
