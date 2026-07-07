namespace MortierFu
{
    public class AGM_Toughness : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod MaxHealthMult;
            public AugmentStatMod MaxHealthFlat;
            public AugmentStatMod FireRateMod;
        }
        
        public AGM_Toughness(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.MaxHealth.AddModifier(db.ToughnessParams.MaxHealthMult.ToMod(this));
            stats.MaxHealth.AddModifier(db.ToughnessParams.MaxHealthFlat.ToMod(this));
            stats.FireRate.AddModifier(db.ToughnessParams.FireRateMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.MaxHealth.RemoveAllModifiersFromSource(this);
            stats.FireRate.RemoveAllModifiersFromSource(this);
        }
    }
}