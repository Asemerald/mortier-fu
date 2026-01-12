namespace MortierFu
{
    public class AGM_BouncyBoomerang : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public float OnBounceUpMinAngle;
            public float OnBounceUpMaxAngle;
            public int ExtraBombshellBounces;
        }
        
        private EventBinding<TriggerBounce> _bounceBinding;
        
        public AGM_BouncyBoomerang(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            _bounceBinding = new EventBinding<TriggerBounce>(OnBounce);
            EventBus<TriggerBounce>.Register(_bounceBinding);
            
            stats.BombshellBounces.AddModifier(new StatModifier(db.BouncyBoomerangParams.ExtraBombshellBounces, E_StatModType.Flat, this));
        }
        
        private void OnBounce(TriggerBounce evt)
        {
            if (evt.Context == null) return;

            evt.Context.UpRotationMinAngle += db.BouncyBoomerangParams.OnBounceUpMinAngle;
            evt.Context.RotationMaxAngle += db.BouncyBoomerangParams.OnBounceUpMaxAngle;
        }

        public override void Dispose()
        {
            EventBus<TriggerBounce>.Deregister(_bounceBinding);

            stats.BombshellBounces.RemoveAllModifiersFromSource(this);
        }
    }
}