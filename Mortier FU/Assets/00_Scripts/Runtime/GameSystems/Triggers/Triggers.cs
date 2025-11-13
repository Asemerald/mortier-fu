namespace MortierFu
{
    public interface ITrigger : IEvent
    { }
    
    public struct TriggerBombshellBounce : ITrigger
    {
        public Bombshell bombshell;
    }
    
    public struct TriggerAiming : ITrigger
    { }
}