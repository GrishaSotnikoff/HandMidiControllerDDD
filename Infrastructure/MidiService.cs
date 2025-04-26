using System;
using HandMidiControllerDDD.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sanford.Multimedia.Midi;

namespace HandMidiControllerDDD.Infrastructure
{
    public class MidiService : IMidiService, IDisposable
    {
        private readonly OutputDevice _device;
        private readonly ILogger<MidiService> _logger;
        private readonly MidiOptions _midiOpts;

        public MidiService(ILogger<MidiService> logger, IOptions<MidiOptions> midiOpts)
        {
            _logger = logger;
            _midiOpts = midiOpts.Value;
            _device = new OutputDevice(midiOpts.Value.DeviceId);
            _logger.LogInformation("🎛️ Connected to MIDI device #{DeviceId}", midiOpts.Value.DeviceId);
        }

        public void SendControlChange(int control, int value)
        {
            // Clamp values safely
            control = Math.Clamp(control, 0, 127);
            value = Math.Clamp(value, 0, 127);

            var msg = new ChannelMessage(ChannelCommand.Controller, _midiOpts.MidiChannel, control, value); // Channel 0 = MIDI Channel 1
            _device.Send(msg);

        }

        public void Dispose()
        {
            _device.Dispose();
            _logger.LogInformation("🔌 MIDI device disconnected.");
        }

        public void SendNoteOn(int note, int velocity)
        {
            var msg = new ChannelMessage(ChannelCommand.NoteOn, _midiOpts.MidiChannel, note, velocity);
            _device.Send(msg);
        }

        public void SendNoteOff(int note)
        {
            var msg = new ChannelMessage(ChannelCommand.NoteOff, _midiOpts.MidiChannel, note, 0);
            _device.Send(msg);
        }

    }
}
