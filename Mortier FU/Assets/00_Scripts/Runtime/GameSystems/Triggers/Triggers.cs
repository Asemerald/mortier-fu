using UnityEngine;

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

    public struct TriggerShootBombshell : ITrigger // Check
    {
        public PlayerCharacter Character;
        public Bombshell Bombshell;
    }

    public struct TriggerBombshellImpact : ITrigger // Check
    {
        public Bombshell Bombshell;
        public GameObject[] Hits;
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
    
    public struct TriggerStrike : ITrigger // Check
    {
        public PlayerCharacter Character;
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
    
    public struct TriggerStartMoving : ITrigger // TODO
    { }
    
    public struct TriggerStopMoving : ITrigger // TODO
    { }

    public struct TriggerGetStrike : ITrigger // Check
    {
        public PlayerCharacter Character;
    }
    
    public struct TriggerEndRound : ITrigger // Check
    { }
}