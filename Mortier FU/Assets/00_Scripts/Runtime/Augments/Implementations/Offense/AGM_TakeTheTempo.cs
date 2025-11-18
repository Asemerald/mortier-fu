namespace MortierFu.Stats
{
    public class AGM_TakeTheTempo : AugmentBase
    {
        private EventBinding<TriggerHit> _hitBinding;
        private EventBinding<TriggerEndRound> _endRoundBinding;
        
        public AGM_TakeTheTempo(SO_Augment augmentData, PlayerCharacter owner) : base(augmentData, owner)
        { }

        public override void Initialize()
        {
            _hitBinding = new EventBinding<TriggerHit>(OnHit);
            EventBus<TriggerHit>.Register(_hitBinding);
            
            _endRoundBinding = new EventBinding<TriggerEndRound>(OnEndRound);
            EventBus<TriggerEndRound>.Register(_endRoundBinding);
        }
        
        private void OnHit(TriggerHit evt)
        {
            if (evt.Bombshell.Owner != owner) return;
            
            stats.FireRate.AddModifier(new StatModifier(-0.2f, E_StatModType.PercentMult, this));
        }
        
        private void OnEndRound(TriggerEndRound evt)
        {
            stats.FireRate.RemoveAllModifiersFromSource(this);
        }
        
        public override void Dispose()
        {
            EventBus<TriggerHit>.Deregister(_hitBinding);
            EventBus<TriggerEndRound>.Deregister(_endRoundBinding);
            stats.FireRate.RemoveAllModifiersFromSource(this);
        }
    }
}