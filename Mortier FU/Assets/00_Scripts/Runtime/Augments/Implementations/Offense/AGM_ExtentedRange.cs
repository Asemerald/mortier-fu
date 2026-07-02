namespace MortierFu
{
    public class AGM_ExtentedRange : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod ShotRangeMod;
        }
        
        public AGM_ExtentedRange(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.ShotRange.AddModifier(db.ExtentedRangeParams.ShotRangeMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.ShotRange.RemoveAllModifiersFromSource(this);
        }
    }
}