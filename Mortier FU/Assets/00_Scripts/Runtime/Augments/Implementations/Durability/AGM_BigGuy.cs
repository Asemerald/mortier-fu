namespace MortierFu
{
    public class AGM_BigGuy : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod MaxHealthMod;
            public AugmentStatMod FireRateMod;
        }
        
        public AGM_BigGuy(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.MaxHealth.AddModifier(db.BigGuyParams.MaxHealthMod.ToMod(this));
            stats.FireRate.AddModifier(db.BigGuyParams.FireRateMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.MaxHealth.RemoveAllModifiersFromSource(this);
            stats.FireRate.RemoveAllModifiersFromSource(this);
        }
    }
}