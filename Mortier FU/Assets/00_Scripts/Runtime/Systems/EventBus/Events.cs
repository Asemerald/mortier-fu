// Code from git-amend "Learn to Build an Advanced Event Bus | Unity Architecture".

namespace MortierFU
{
    public interface IEvent 
    { }
    
    public class Events : IEvent{ }

    public struct TriggerEvent : IEvent
    { }
}