using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HandMidiControllerDDD.Application.Interfaces;
using HandMidiControllerDDD.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace HandMidiControllerDDD.Infrastructure
{
    public class CameraService : ICameraService, IDisposable
    {
        private readonly ILogger<CameraService> _logger;
        private readonly IMidiService _midi;
        private readonly CancellationTokenSource _cts = new();
        private double _lastX, _lastY, _lastZ;

        // Gesture-to-CC mapping: (ccX, ccY)
        private static readonly System.Collections.Generic.Dictionary<string, (int ccX, int ccY)> _mapping =
            new()
            {
                ["fist"] = (21, 22),
                ["open"] = (23, 24),
                ["point"] = (71, 72),
                ["victory"] = (73, 74),
                ["rock"] = (91, 92),
            };

        public CameraService(ILogger<CameraService> logger, IMidiService midi)
        {
            _logger = logger;
            _midi = midi;
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
                    await ws.ConnectAsync(new Uri("ws://localhost:8765"), token);
                    _logger.LogInformation("Connected to gesture server.");

                    while (ws.State == WebSocketState.Open && !token.IsCancellationRequested)
                    {
                        var result = await ws.ReceiveAsync(buffer, token);
                        if (result.MessageType == WebSocketMessageType.Close)
                            break;

                        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        var frame = JsonSerializer.Deserialize<GestureFrame>(json);

                        if (frame.Detected == true)
                        {
                            _lastX = frame.x;
                            _lastY = frame.y;
                            _lastZ = 0;
                            if (_mapping.TryGetValue(frame.Gesture, out var pair))
                            {
                                SendGestureMidi(pair.ccX, pair.ccY, frame.x, frame.y, frame.Gesture);
                            }
                            else
                            {
                                _logger.LogDebug("Skipping unknown gesture '{Gesture}'", frame.Gesture);
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "WebSocket error, retrying...");
                }
                // wait before reconnect
                try { await Task.Delay(3000, token); } catch { break; }
            }
        }

        private void SendGestureMidi(int ccX, int ccY, double x, double y, string gesture)
        {
            int vX = Clamp(x), vY = Clamp(y);
            _midi.SendControlChange(ccX, vX);
            _midi.SendControlChange(ccY, vY);
            _logger.LogInformation("Gesture '{Gesture}': CC#{CcX}={ValX}, CC#{CcY}={ValY}", gesture, ccX, vX, ccY, vY);
        }

        private static int Clamp(double v) => Math.Clamp((int)(v * 127), 0, 127);

        public HandPosition GetHandPosition() => new(_lastX, _lastY, _lastZ);

        public void Dispose()
        {
            _cts.Cancel();
            _logger.LogInformation("CameraService disposed.");
        }

        private class GestureFrame {
  
            public bool Detected { get; set; } public string Gesture { get; set; } public double x { get; set; } public double y { get; set; } }
    }
}
