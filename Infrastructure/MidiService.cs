using System;
using HandMidiControllerDDD.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Sanford.Multimedia.Midi;

namespace HandMidiControllerDDD.Infrastructure
{
    public class MidiService : IMidiService, IDisposable
    {
        private readonly OutputDevice _device;
        private readonly ILogger<MidiService> _logger;

        public MidiService(ILogger<MidiService> logger, int deviceId = 1)
        {
            _logger = logger;
            _device = new OutputDevice(deviceId);
            _logger.LogInformation("🎛️ Connected to MIDI device #{DeviceId}", deviceId);
        }

        public void SendControlChange(int control, int value)
        {
            // Clamp values safely
            control = Math.Clamp(control, 0, 127);
            value = Math.Clamp(value, 0, 127);

            var msg = new ChannelMessage(ChannelCommand.Controller, 0, control, value); // Channel 0 = MIDI Channel 1
            _device.Send(msg);

        }

        public void Dispose()
        {
            _device.Dispose();
            _logger.LogInformation("🔌 MIDI device disconnected.");
        }

        public void SendNoteOn(int note, int velocity)
        {
            var msg = new ChannelMessage(ChannelCommand.NoteOn, 0, note, velocity);
            _device.Send(msg);
        }

        public void SendNoteOff(int note)
        {
            var msg = new ChannelMessage(ChannelCommand.NoteOff, 0, note, 0);
            _device.Send(msg);
        }

    }
}
