using System;
using HandMidiControllerDDD.Domain.ValueObjects;

namespace HandMidiControllerDDD.Domain.Entities
{
    public class Gesture
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string Name { get; private set; }
        public HandPosition Position { get; private set; }

        public Gesture(string name, HandPosition position)
        {
            Name = name;
            Position = position;
        }

        public void UpdatePosition(HandPosition newPos) => Position = newPos;
    }
}