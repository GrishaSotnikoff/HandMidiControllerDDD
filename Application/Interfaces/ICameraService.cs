using HandMidiControllerDDD.Domain.ValueObjects;

namespace HandMidiControllerDDD.Application.Interfaces
{
    public interface ICameraService
    {
        HandPosition GetHandPosition();
    }
}