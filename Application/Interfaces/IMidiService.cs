namespace HandMidiControllerDDD.Application.Interfaces
{
    public interface IMidiService
    {
        void SendControlChange(int control, int value);
        public void SendNoteOff(int note);
        public void SendNoteOn(int note, int velocity);
    }
}