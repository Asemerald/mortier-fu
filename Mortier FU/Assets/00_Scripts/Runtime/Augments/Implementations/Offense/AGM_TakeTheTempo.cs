namespace MortierFu
{
    public class AGM_TakeTheTempo : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod FireRateMod;
            public float MaxStacksTempo;
        }
        
        private EventBinding<TriggerHit> _hitBinding;
        private EventBinding<TriggerEndRound> _endRoundBinding;

        private int stackAmountTempo;
        
        public AGM_TakeTheTempo(SO_Augment augmentData, PlayerCharacter owner,  SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            _hitBinding = new EventBinding<TriggerHit>(OnHit);
            EventBus<TriggerHit>.Register(_hitBinding);
            
            _endRoundBinding = new EventBinding<TriggerEndRound>(OnEndRound);
            EventBus<TriggerEndRound>.Register(_endRoundBinding);
            
            stackAmountTempo = 0;
        }
        
        private void OnHit(TriggerHit evt)
        {
            if (evt.Bombshell.Owner != owner) return;
            
            if (stackAmountTempo <= db.TakeTheTempoParams.MaxStacksTempo)
            {
                stats.FireRate.AddModifier(db.TakeTheTempoParams.FireRateMod.ToMod(this));
                AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Augment_Buff, owner.transform.position);
                stackAmountTempo++;
            }
            
            
        }
        
        private void OnEndRound(TriggerEndRound evt)
        {
            stats.FireRate.RemoveAllModifiersFromSource(this);
            stackAmountTempo = 0;
        }
        
        public override void Dispose()
        {
            EventBus<TriggerHit>.Deregister(_hitBinding);
            EventBus<TriggerEndRound>.Deregister(_endRoundBinding);
            stats.FireRate.RemoveAllModifiersFromSource(this);
        }
    }
}