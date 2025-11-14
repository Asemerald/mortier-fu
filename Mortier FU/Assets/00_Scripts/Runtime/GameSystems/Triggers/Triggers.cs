namespace MortierFu
{
    public interface ITrigger : IEvent
    { }
    
    public struct TriggerStartAimed: ITrigger
    { }
    
    public struct TriggerStopAimed : ITrigger
    { }
    
    public struct TriggerStackCharged : ITrigger
    { }
    
    public struct TriggerFullCharged : ITrigger
    { }
    
    public struct TriggerShootBombshell : ITrigger
    { }
    
    public struct TriggerBombshellLanded : ITrigger
    { }
    
    public struct TriggerBombshellImpacted : ITrigger
    { }

    public struct TriggerHit : ITrigger
    {
        public Bombshell Bombshell;
        public PlayerCharacter[] HitCharacters;
    }
    
    public struct TriggerHealthChanged : ITrigger
    {
        public PlayerCharacter Character;
        public float PreviousHealth;
        public float NewHealth;
        public float MaxHealth;
        public float Delta;
    }
    
    public struct TriggerStrikeHit : ITrigger
    {
        public PlayerCharacter Character;
        public PlayerCharacter[] HitCharacters;
    }

    public struct TriggerStrikeHitBombshell : ITrigger
    {
        public PlayerCharacter Character;
        public Bombshell[] HitBombshells;
    }
    
    public struct TriggerStopMoved : ITrigger
    { }
    
    public struct TriggerGetStrike : ITrigger
    { }
}