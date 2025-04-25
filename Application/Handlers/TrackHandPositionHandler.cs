using MediatR;
using HandMidiControllerDDD.Application.Commands;
using HandMidiControllerDDD.Application.Interfaces;
using System.Threading.Tasks;
using System.Threading;

namespace HandMidiControllerDDD.Application.Handlers
{
    public class TrackHandPositionHandler : IRequestHandler<TrackHandPositionCommand>
    {
        private readonly IMidiService _midiService;

        public TrackHandPositionHandler(IMidiService midiService)
        {
            _midiService = midiService;
        }

        public Task<Unit> Handle(TrackHandPositionCommand request, CancellationToken cancellationToken)
        {
            var x = (int)(request.Position.X * 127);
            _midiService.SendControlChange(1, x);
            return Unit.Task;
        }
    }
}