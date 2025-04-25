using MediatR;
using HandMidiControllerDDD.Domain.ValueObjects;

namespace HandMidiControllerDDD.Application.Commands
{
    public record TrackHandPositionCommand(HandPosition Position) : IRequest;
}