namespace MortierFu
{
    public class AGM_Bouncy : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod BombShellDamageMod;
            public AugmentStatMod BombshellBouncesMod;
            //public float OnBounceSizeMod;
        }
        
        //private EventBinding<TriggerBounce> _bounceBinding;
        
        public AGM_Bouncy(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.BombshellDamage.AddModifier(db.BouncyParams.BombShellDamageMod.ToMod(this));
            stats.BombshellBounces.AddModifier(db.BouncyParams.BombshellBouncesMod.ToMod(this));

            // _bounceBinding = new EventBinding<TriggerBounce>(OnBounce);
            // EventBus<TriggerBounce>.Register(_bounceBinding);
        }

        public override void Dispose()
        {
            stats.FireRate.RemoveAllModifiersFromSource(this);
            stats.BombshellBounces.RemoveAllModifiersFromSource(this);
            
            //EventBus<TriggerBounce>.Deregister(_bounceBinding);
        }
        
        // private void OnBounce(TriggerBounce evt)
        // {
        //     evt.Bombshell.MultiplyScale(1 + db.BouncyParams.OnBounceSizeMod);
        //     evt.Bombshell.AoeRange *= 1 + db.BouncyParams.OnBounceSizeMod;
        // }
    }
}