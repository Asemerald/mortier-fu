namespace MortierFu
{
    public class AGM_Overheating : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod BombshellTimeTravelMod;
            public AugmentStatMod BombshellTimeTravelOnHitMod;
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
            
            stats.BombshellTimeTravel.AddModifier(db.OverheatingParams.BombshellTimeTravelMod.ToMod(this));
        }
        
        private void OnShoot(TriggerShootBombshell evt)
        {
            if (evt.Character != owner) return;
            
            stats.BombshellTimeTravel.AddModifier(db.OverheatingParams.BombshellTimeTravelOnHitMod.ToMod(this));
        }
        
        private void OnEndRound(TriggerEndRound evt)
        {
            stats.BombshellTimeTravel.RemoveAllModifiersFromSource(this);
            stats.BombshellTimeTravel.AddModifier(db.OverheatingParams.BombshellTimeTravelMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            EventBus<TriggerShootBombshell>.Deregister(_shootBinding);
            EventBus<TriggerEndRound>.Deregister(_endRoundBinding);
            stats.BombshellTimeTravel.RemoveAllModifiersFromSource(this);
        }
    }
}