// Code from git-amend "Learn to Build an Advanced Event Bus | Unity Architecture".

namespace MortierFu
{
    public interface IEvent 
    { }

    public struct EventSystemDisposed : IEvent
    {
        public IGameSystem System;
    }

    public struct EventPlayerDeath : IEvent {
        public PlayerCharacter Character;
        public object Source;
    }
}