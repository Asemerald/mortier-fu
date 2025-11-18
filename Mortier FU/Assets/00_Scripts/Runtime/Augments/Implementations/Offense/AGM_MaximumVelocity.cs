namespace MortierFu
{
    public class AGM_MaximumVelocity : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod BombshellTimeTravelMod;
            public AugmentStatMod FireRateMod;
        }
        
        public AGM_MaximumVelocity(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.BombshellTimeTravel.AddModifier(db.MaximumVelocityParams.BombshellTimeTravelMod.ToMod(this));
            stats.FireRate.AddModifier(db.MaximumVelocityParams.FireRateMod.ToMod(this));
        }
            
        public override void Dispose()
        {
            stats.BombshellTimeTravel.RemoveAllModifiersFromSource(this);
            stats.FireRate.RemoveAllModifiersFromSource(this);
        }
    }
}