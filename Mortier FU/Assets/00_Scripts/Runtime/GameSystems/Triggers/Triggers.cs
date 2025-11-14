namespace MortierFu
{
    public interface ITrigger : IEvent
    { }
    
    public struct TriggerHealthChanged : ITrigger
    {
        public PlayerCharacter Character;
        public float PreviousHealth;
        public float NewHealth;
        public float MaxHealth;
        public float Delta;
    }
}