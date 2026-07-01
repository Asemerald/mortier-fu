namespace MortierFu
{
    public class AGM_Impact : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod BombshellImpactRadiusMod;
        }
        
        public AGM_Impact(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.BombshellImpactRadius.AddModifier(db.ImpactParams.BombshellImpactRadiusMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.BombshellImpactRadius.RemoveAllModifiersFromSource(this);
        }
    }
}