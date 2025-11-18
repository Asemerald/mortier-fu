namespace MortierFu
{
    public class AGM_PerfectParry : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod StrikeCooldownMod;
            public AugmentStatMod MaxHealthMod;
            public AugmentStatMod ParryMaxHealthGain;
        }
        
        private EventBinding<TriggerStrikeHitBombshell> _strikeHitBinding;
        private EventBinding<TriggerEndRound> _endRoundBinding;
        
        public AGM_PerfectParry(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            _strikeHitBinding = new EventBinding<TriggerStrikeHitBombshell>(OnTriggerStrikeHitBombshell);
            EventBus<TriggerStrikeHitBombshell>.Register(_strikeHitBinding);

            _endRoundBinding = new EventBinding<TriggerEndRound>(OnTriggerEndRound);
            EventBus<TriggerEndRound>.Register(_endRoundBinding);
            
            stats.StrikeCooldown.AddModifier(db.PerfectParryParams.StrikeCooldownMod.ToMod(this));
            stats.MaxHealth.AddModifier(db.PerfectParryParams.MaxHealthMod.ToMod(this));
        }
        
        private void OnTriggerStrikeHitBombshell(TriggerStrikeHitBombshell evt)
        {
            if (evt.Character != owner) return;

            stats.MaxHealth.AddModifier(db.PerfectParryParams.ParryMaxHealthGain.ToMod(this));
        }

        private void OnTriggerEndRound(TriggerEndRound evt)
        {
            stats.MaxHealth.RemoveAllModifiersFromSource(this);
            stats.StrikeCooldown.AddModifier(db.PerfectParryParams.StrikeCooldownMod.ToMod(this));
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