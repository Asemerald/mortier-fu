namespace MortierFu
{
    public class AGM_Overheating : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod BombshellSpeedMod;
            public AugmentStatMod OnHitBombshellSpeedMod;
        }
        
        private EventBinding<TriggerShootBombshell> _shootBinding;
        private EventBinding<TriggerEndRound> _endRoundBinding;
        
        public AGM_Overheating(SO_Augment augmentData, PlayerCharacter owner,  SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }
        
        public override void Initialize()
        {
            _shootBinding = new EventBinding<TriggerShootBombshell>(OnShoot);
            EventBus<TriggerShootBombshell>.Register(_shootBinding);
            
            _endRoundBinding = new EventBinding<TriggerEndRound>(OnEndRound);
            EventBus<TriggerEndRound>.Register(_endRoundBinding);
            
            stats.BombshellSpeed.AddModifier(db.OverheatingParams.BombshellSpeedMod.ToMod(this));
        }
        
        private void OnShoot(TriggerShootBombshell evt)
        {
            if (evt.Character != owner) return;
            
            stats.BombshellSpeed.AddModifier(db.OverheatingParams.OnHitBombshellSpeedMod.ToMod(this));
        }
        
        private void OnEndRound(TriggerEndRound evt)
        {
            stats.BombshellSpeed.RemoveAllModifiersFromSource(this);
            stats.BombshellSpeed.AddModifier(db.OverheatingParams.BombshellSpeedMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            EventBus<TriggerShootBombshell>.Deregister(_shootBinding);
            EventBus<TriggerEndRound>.Deregister(_endRoundBinding);
            stats.BombshellSpeed.RemoveAllModifiersFromSource(this);
        }
    }
}