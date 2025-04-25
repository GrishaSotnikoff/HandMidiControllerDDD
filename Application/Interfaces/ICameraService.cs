using System;
using HandMidiControllerDDD.Domain.ValueObjects;

namespace HandMidiControllerDDD.Application.Interfaces
{
    public interface ICameraService : IDisposable
    {
        HandPosition GetHandPosition();
    }
}