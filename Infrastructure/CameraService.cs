using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HandMidiControllerDDD.Application.Interfaces;
using HandMidiControllerDDD.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;

namespace HandMidiControllerDDD.Infrastructure
{
    public class CameraService : ICameraService, IDisposable
    {
        private readonly string _wsAddress;
        private readonly ILogger<CameraService> _logger;
        private readonly IMidiService _midi;
        private readonly MetricPusher _pusher;
        private readonly Counter _eventCounter;
        private readonly CancellationTokenSource _cts = new();

        // Last hand position
        private double _lastX, _lastY, _lastZ;

        // Gesture-to-CC mapping
        private static readonly System.Collections.Generic.Dictionary<string, (int Xcc, int Ycc)> _ccMap = new()
        {
            ["fist"] = (21, 22),
            ["open"] = (23, 83),
            ["point"] = (93, 10),
            ["victory"] = (73, 74),
            ["rock"] = (91, 92),
        };

        public CameraService(
            ILogger<CameraService> logger,
            IMidiService midi,
            IOptions<WebSocketOptions> wsOpts,
            MetricPusher pusher)
        {
            _logger = logger;
            _midi = midi;
            _wsAddress = wsOpts.Value.Address;
            _pusher = pusher;

            // Create and register our Prometheus counter
            _eventCounter = Metrics
                .CreateCounter("hand_event_total", "Total number of hand-tracking events");

            // Start the Pushgateway client loop (will post at its configured interval)
            _pusher.Start();  // ← use Start(), not Push() :contentReference[oaicite:0]{index=0}

            // Fire-and-forget our WS receive loop
            _ = RunLoopAsync(_cts.Token);
        }

        private async Task RunLoopAsync(CancellationToken token)
        {
            var buffer = new byte[512];

            while (!token.IsCancellationRequested)
            {
                using var ws = new ClientWebSocket();
                try
                {
                    await ws.ConnectAsync(new Uri(_wsAddress), token);
                    _logger.LogInformation("Connected to gesture WS at {Url}", _wsAddress);

                    while (ws.State == WebSocketState.Open && !token.IsCancellationRequested)
                    {
                        var res = await ws.ReceiveAsync(buffer, token);
                        if (res.MessageType == WebSocketMessageType.Close) break;

                        var json = Encoding.UTF8.GetString(buffer, 0, res.Count);
                        var frame = JsonSerializer.Deserialize<GestureFrame>(json);
                        if (frame?.Detected == true)
                        {
                            _lastX = frame.x;
                            _lastY = frame.y;
                            _lastZ = frame.z;

                            if (_ccMap.TryGetValue(frame.Gesture, out var cc))
                            {
                                int vX = Clamp(_lastX);
                                int vY = Clamp(_lastY);

                                _midi.SendControlChange(cc.Xcc, vX);
                                _midi.SendControlChange(cc.Ycc, vY);

                                _logger.LogDebug(
                                  "{Gesture}: CC#{Xcc}={ValX}, CC#{Ycc}={ValY}",
                                  frame.Gesture, cc.Xcc, vX, cc.Ycc, vY);
                            }

                            // bump the Prom counter (Pushgateway client will pick it up)
                            _eventCounter.Inc();
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "WS error, retrying in 3s…");
                }

                try { await Task.Delay(3000, token); } catch { break; }
            }
        }

        public HandPosition GetHandPosition() =>
            new(_lastX, _lastY, _lastZ);

        private static int Clamp(double v) =>
            Math.Clamp((int)(v * 127), 0, 127);

        public void Dispose()
        {
            _cts.Cancel();
            _logger.LogInformation("CameraService disposed.");
        }

        private class GestureFrame
        {
            public bool Detected { get; set; }
            public string Gesture { get; set; }
            public double x { get; set; }
            public double y { get; set; }
            public double z { get; set; }
        }
    }
}
