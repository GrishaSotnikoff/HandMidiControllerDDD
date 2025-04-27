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
    public class RabbitMqOptions
    {
        public string Hostname { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string VirtualHost { get; set; } = "/";
        public string Username { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string ExchangeName { get; set; } = "midi.events";
        public bool AlsoSendToLocalDevice { get; set; } = true;
    }
}