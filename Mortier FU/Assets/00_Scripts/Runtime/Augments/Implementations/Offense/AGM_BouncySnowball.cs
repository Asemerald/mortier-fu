namespace MortierFu
{
    public class AGM_BouncySnowball : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod BombshellImpactRadius;
            public float OnBounceImpactRadiusScalar;
            public int ExtraBombshellBounces;
        }
        
        private EventBinding<TriggerBounce> _bounceBinding;
        
        public AGM_BouncySnowball(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            _bounceBinding = new EventBinding<TriggerBounce>(OnBounce);
            EventBus<TriggerBounce>.Register(_bounceBinding);
            
            int extraBombshells = db.BouncySnowballParams.ExtraBombshellBounces;
            
            stats.BombshellImpactRadius.AddModifier(db.BouncySnowballParams.BombshellImpactRadius.ToMod(this));
            stats.BombshellBounces.AddModifier(new StatModifier(extraBombshells, E_StatModType.Flat, this));
        }
        
        private void OnBounce(TriggerBounce evt)
        {
            if (evt.Bombshell == null || evt.Bombshell.Owner != owner) return;
            
            float scalar = 1f + db.BouncySnowballParams.OnBounceImpactRadiusScalar;
            
            evt.Bombshell.AoeRange *= scalar;
            evt.Bombshell.MultiplyScale(scalar);
        }

        
        public override void Dispose()
        {
            EventBus<TriggerBounce>.Deregister(_bounceBinding);

            stats.BombshellImpactRadius.RemoveAllModifiersFromSource(this);
            stats.BombshellBounces.RemoveAllModifiersFromSource(this);
        }
    }

}