namespace MortierFu
{
    public interface ITrigger : IEvent
    { }
    
    public struct TriggerStartAimed: ITrigger // TODO
    { }
    
    public struct TriggerStopAimed : ITrigger // TODO
    { }
    
    public struct TriggerStackCharged : ITrigger // TODO
    { }
    
    public struct TriggerFullCharged : ITrigger // TODO
    { }
    
    public struct TriggerShootBombshell : ITrigger // TODO
    { }
    
    public struct TriggerBombshellLanded : ITrigger // TODO
    { }

    public struct TriggerBombshellImpacted : ITrigger // TODO
    {
        public Bombshell Bombshell;
    }

    public struct TriggerHit : ITrigger // Check
    {
        public Bombshell Bombshell;
        public PlayerCharacter[] HitCharacters;
    }
    
    public struct TriggerHealthChanged : ITrigger // Check
    {
        public PlayerCharacter Character;
        public float PreviousHealth;
        public float NewHealth;
        public float MaxHealth;
        public float Delta;
    }
    
    public struct TriggerStrikeHit : ITrigger // Check
    {
        public PlayerCharacter Character;
        public PlayerCharacter[] HitCharacters;
    }

    public struct TriggerStrikeHitBombshell : ITrigger // Check
    {
        public PlayerCharacter Character;
        public Bombshell[] HitBombshells;
    }
    
    public struct TriggerStopMoved : ITrigger // TODO
    { }
    
    public struct TriggerGetStrike : ITrigger // TODO
    { }
    
    public struct TriggerEndRound : ITrigger // Check
    { }
}