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
        private readonly ClientWebSocket _ws;
        private readonly ILogger<CameraService> _logger;
        private readonly IMidiService _midi;
        private readonly CancellationTokenSource _cts = new();
        private double _x, _y, _z;

        public CameraService(ILogger<CameraService> logger, IMidiService midi)
        {
            _logger = logger;
            _midi = midi;
            _ws = new ClientWebSocket();
            _ = ConnectAndReceiveLoopAsync(_cts.Token);
        }

        private async Task ConnectAndReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                await _ws.ConnectAsync(new Uri("ws://localhost:8765"), token);
                _logger.LogInformation("✨ Connected to Python hand server.");

                var buffer = new byte[4096];
                while (_ws.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    var seg = new ArraySegment<byte>(buffer);
                    var result = await _ws.ReceiveAsync(seg, token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("Server closed connection.");
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        try
                        {
                            var frame = JsonSerializer.Deserialize<Frame>(json)!;

                            if (frame.detected && frame.fingers != null)
                            {
                                for (int i = 0; i < frame.fingers.Length; i++)
                                {
                                    var finger = frame.fingers[i];
                                    int ccValue = ClampMidi(1.0 - finger.y); // invert Y: hand higher = higher CC
                                    int ccNumber = 22 + i; // Thumb starts at CC21

                                    _midi.SendControlChange(ccNumber, ccValue);
                                    _logger.LogInformation("🎛️ Finger {0} → CC#{1} → {2}", i, ccNumber, ccValue);
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogError(ex, "Invalid JSON from hand_server: {Json}", json);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebSocket connect/receive error");
            }
        }

        private class Frame
        {
            public FingerCoord[] fingers { get; set; }
            public bool detected { get; set; }
        }

        private class FingerCoord
        {
            public double x { get; set; }
            public double y { get; set; }
            public double z { get; set; }
        }

        private int ClampMidi(double val) =>
            Math.Clamp((int)(val * 127), 0, 127);

        public HandPosition GetHandPosition() => new(_x, _y, _z);

        public void Dispose()
        {
            _cts.Cancel();
            if (_ws.State == WebSocketState.Open)
                _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "disposing", CancellationToken.None).Wait();
            _ws.Dispose();
            _logger.LogInformation("CameraService disposed.");
        }

        private class Coord
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
            public bool detected { get; set; }
        }
    }
}
