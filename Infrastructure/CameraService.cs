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

        // Last hand position and gesture
        private double _lastX, _lastY, _lastZ;
        private string _prevGesture = string.Empty;

        // Gesture-to-CC mapping
        private static readonly System.Collections.Generic.Dictionary<string, (int Xcc, int Ycc)> _ccMap = new()
        {
            ["fist"] = (21, 22),
            ["open"] = (23, 83),
            ["point"] = (93, 10),
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
                        var res = await ws.ReceiveAsync(buffer, token);
                        if (res.MessageType == WebSocketMessageType.Close) break;

                        var json = Encoding.UTF8.GetString(buffer, 0, res.Count);
                        var frame = JsonSerializer.Deserialize<GestureFrame>(json);
                        if (frame.Detected == true)
                        {
                            _lastX = frame.x;
                            _lastY = frame.y;
                            _lastZ = frame.z; // if server sends Z, else 0

                            // On gesture change, trigger note
                            //if (frame.Gesture != _prevGesture)
                            //{
                            //    TriggerGestureNote(frame.Gesture, _lastX, _lastY, _lastZ);
                            //    _prevGesture = frame.Gesture;
                            //}

                            // Always send CCs for current gesture
                            if (_ccMap.TryGetValue(frame.Gesture, out var cc))
                            {
                                int vX = Clamp(_lastX);
                                int vY = Clamp(_lastY);
                                _midi.SendControlChange(cc.Xcc, vX);
                                _midi.SendControlChange(cc.Ycc, vY);
                                _logger.LogDebug("{Gesture}: CC#{Xcc}={ValX}, CC#{Ycc}={ValY}", frame.Gesture, cc.Xcc, vX, cc.Ycc, vY);
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "WebSocket error, retrying...");
                }

                // delay before reconnect
                try { await Task.Delay(3000, token); } catch { break; }
            }
        }

        private void TriggerGestureNote(string gesture, double x, double y, double z)
        {
            // Map axes to note number and velocity
            int note = MapToNote(x, y, z);
            int vel = Clamp(y);

            _midi.SendNoteOn(note, vel);
            // schedule note off after 200ms
            Task.Delay(200).ContinueWith(_ => _midi.SendNoteOff(note));

            _logger.LogInformation("Gesture '{Gesture}' changed → NoteOn {Note} Vel {Vel}", gesture, note, vel);
        }

        private static int MapToNote(double x, double y, double z)
        {
            // combine normalized axes to a note in range C3 (48) to C6 (84)
            double avg = (x + y + z) / 3.0;
            int note = 48 + Clamp(avg) * 36 / 127;
            return Math.Clamp(note, 0, 127);
        }

        private static int Clamp(double v) => Math.Clamp((int)(v * 127), 0, 127);

        public HandPosition GetHandPosition() => new(_lastX, _lastY, _lastZ);

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