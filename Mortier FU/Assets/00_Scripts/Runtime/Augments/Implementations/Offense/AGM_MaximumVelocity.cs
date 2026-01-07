namespace MortierFu
{
    public class AGM_MaximumVelocity : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod BombshellSpeedMod;
            public AugmentStatMod FireRateMod;
        }
        
        public AGM_MaximumVelocity(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.BombshellSpeed.AddModifier(db.MaximumVelocityParams.BombshellSpeedMod.ToMod(this));
            stats.FireRate.AddModifier(db.MaximumVelocityParams.FireRateMod.ToMod(this));
        }
            
        public override void Dispose()
        {
            stats.BombshellSpeed.RemoveAllModifiersFromSource(this);
            stats.FireRate.RemoveAllModifiersFromSource(this);
        }
    }
}