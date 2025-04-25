using HandMidiControllerDDD.Domain.Entities;
using HandMidiControllerDDD.Domain.ValueObjects;

namespace HandMidiControllerDDD.Domain.Services
{
    public interface IHandTrackingService
    {
        Gesture TrackHand(double x, double y, double z);
    }

    public class HandTrackingService : IHandTrackingService
    {
        public Gesture TrackHand(double x, double y, double z)
        {
            var position = new HandPosition(x, y, z);
            return new Gesture("IndexFinger", position);
        }
    }
}