namespace HandMidiControllerDDD.Infrastructure
{
    public class WebSocketOptions
    {
        public string Address { get; set; } = "";
    }

    public class PrometheusOptions
    {
        public string PushGatewayUrl { get; set; } = "";
        public string JobName { get; set; } = "hand_midi_controller";
    }
    public class MidiOptions
    {
        public int DeviceId { get; set; } = 0;
        public int MidiChannel { get; set; } = 0;
    }
}