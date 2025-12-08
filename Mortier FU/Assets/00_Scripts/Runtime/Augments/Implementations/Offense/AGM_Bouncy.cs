namespace MortierFu
{
    public class AGM_Bouncy : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod FireRateMod;
            public AugmentStatMod BombshellBouncesMod;
        }
        
        public AGM_Bouncy(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.FireRate.AddModifier(db.BouncyParams.FireRateMod.ToMod(this));
            stats.BombshellBounces.AddModifier(db.BouncyParams.BombshellBouncesMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.FireRate.RemoveAllModifiersFromSource(this);
            stats.BombshellBounces.RemoveAllModifiersFromSource(this);
        }
    }
}