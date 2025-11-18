namespace MortierFu
{
    public class AGM_FastReload : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod FireRateMod;
        }
        
        public AGM_FastReload(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.FireRate.AddModifier(db.FastReloadParams.FireRateMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.FireRate.RemoveAllModifiersFromSource(this);
        }
    }
}