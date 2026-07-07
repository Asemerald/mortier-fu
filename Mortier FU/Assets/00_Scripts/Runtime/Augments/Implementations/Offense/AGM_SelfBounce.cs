namespace MortierFu
{
    public class AGM_SelfBounce : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public int ExtraBombshellBounces;
            public AugmentStatMod ShotRangeMod;
           // public AugmentStatMod BombshellSpeedMod;
            public float ImpactBombshellSpeed;
        }

        private EventBinding<TriggerBounce> _bounceBinding;

        public AGM_SelfBounce(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            _bounceBinding = new EventBinding<TriggerBounce>(OnBounce);
            EventBus<TriggerBounce>.Register(_bounceBinding);

            stats.ShotRange.AddModifier(db.SelfBounceParams.ShotRangeMod.ToMod(this));
           // stats.BombshellSpeed.AddModifier(db.SelfBounceParams.BombshellSpeedMod.ToMod(this));
            stats.BombshellBounces.AddModifier(new StatModifier(db.SelfBounceParams.ExtraBombshellBounces, E_StatModType.Flat, this));
        }

        private void OnBounce(TriggerBounce evt)
        {
            if (!evt.Bombshell || evt.Bombshell.Owner != owner || evt.Context == null)
                return;

            float add = db.SelfBounceParams.ImpactBombshellSpeed;

            evt.Bombshell.Speed += add;
            
            evt.Context.ForceInPlaceBounce = true;
        }

        public override void Dispose()
        {
            if (_bounceBinding != null)
                EventBus<TriggerBounce>.Deregister(_bounceBinding);

            stats.BombshellBounces.RemoveAllModifiersFromSource(this);
            stats.BombshellSpeed.RemoveAllModifiersFromSource(this);
            stats.ShotRange.RemoveAllModifiersFromSource(this);
        }
    }
}