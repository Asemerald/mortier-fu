namespace MortierFu
{
    public class AGM_Toughness : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod MaxHealthMod;
            public AugmentStatMod FireRateMod;
        }
        
        public AGM_Toughness(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.MaxHealth.AddModifier(db.ToughnessParams.MaxHealthMod.ToMod(this));
            stats.FireRate.AddModifier(db.ToughnessParams.FireRateMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.MaxHealth.RemoveAllModifiersFromSource(this);
            stats.FireRate.RemoveAllModifiersFromSource(this);
        }
    }
}