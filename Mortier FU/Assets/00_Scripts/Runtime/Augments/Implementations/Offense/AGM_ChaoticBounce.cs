namespace MortierFu
{
    public class AGM_ChaoticBounce : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public int ExtraBombshellBounces;
            public AugmentStatMod ShotRangeMod;
        }

        private EventBinding<TriggerBounce> _bounceBinding;

        public AGM_ChaoticBounce(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            _bounceBinding = new EventBinding<TriggerBounce>(OnBounce);
            EventBus<TriggerBounce>.Register(_bounceBinding);

            stats.ShotRange.AddModifier(db.ChaoticBounceParams.ShotRangeMod.ToMod(this));
            stats.BombshellBounces.AddModifier(new StatModifier(db.ChaoticBounceParams.ExtraBombshellBounces, E_StatModType.Flat, this));
        }

        private void OnBounce(TriggerBounce evt)
        {
            if (!evt.Bombshell || evt.Bombshell.Owner != owner || evt.Context == null)
                return;

            evt.Context.ForceInPlaceBounce = true;
        }

        public override void Dispose()
        {
            if (_bounceBinding != null)
                EventBus<TriggerBounce>.Deregister(_bounceBinding);

            stats.BombshellBounces.RemoveAllModifiersFromSource(this);
            stats.ShotRange.RemoveAllModifiersFromSource(this);
        }
    }
}