namespace MortierFu
{
    public class AGM_Sniper : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod ShotRangeMod;
        }
        
        public AGM_Sniper(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.ShotRange.AddModifier(db.SniperParams.ShotRangeMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.ShotRange.RemoveAllModifiersFromSource(this);
        }
    }
}