namespace MortierFu.Stats
{
    public class AGM_PerfectParry : AugmentBase
    {
        private EventBinding<TriggerStrikeHitBombshell> _strikeHitBinding;
        private EventBinding<TriggerEndRound> _endRoundBinding;
        
        public AGM_PerfectParry(SO_Augment augmentData, PlayerCharacter owner) : base(augmentData, owner)
        { }

        public override void Initialize()
        {
            _strikeHitBinding = new EventBinding<TriggerStrikeHitBombshell>(OnTriggerStrikeHitBombshell);
            EventBus<TriggerStrikeHitBombshell>.Register(_strikeHitBinding);

            _endRoundBinding = new EventBinding<TriggerEndRound>(OnTriggerEndRound);
            EventBus<TriggerEndRound>.Register(_endRoundBinding);
            
            stats.StrikeCooldown.AddModifier(new StatModifier(-0.1f, E_StatModType.PercentMult, this));
            stats.MaxHealth.AddModifier(new StatModifier(-2.0f, E_StatModType.Flat, this));
        }
        
        private void OnTriggerStrikeHitBombshell(TriggerStrikeHitBombshell evt)
        {
            if (evt.Character != owner) return;

            stats.MaxHealth.AddModifier(new StatModifier(1.0f, E_StatModType.Flat, this));
        }

        private void OnTriggerEndRound(TriggerEndRound evt)
        {
            stats.MaxHealth.RemoveAllModifiersFromSource(this);
            stats.MaxHealth.AddModifier(new StatModifier(-2.0f, E_StatModType.Flat, this));
        }
        
        public override void Dispose()
        {
            EventBus<TriggerStrikeHitBombshell>.Deregister(_strikeHitBinding);
            EventBus<TriggerEndRound>.Deregister(_endRoundBinding);
            
            stats.MaxHealth.RemoveAllModifiersFromSource(this);
            stats.StrikeCooldown.RemoveAllModifiersFromSource(this);
        }
    }
}