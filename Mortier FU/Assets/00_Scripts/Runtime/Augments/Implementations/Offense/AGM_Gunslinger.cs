namespace MortierFu
{
    public class AGM_Gunslinger : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod FireRateMod;
            public AugmentStatMod ShotRangeMod;
            public AugmentStatMod HealthLooseMod;
        }
        
        public AGM_Gunslinger(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.FireRate.AddModifier(db.GunslingerParams.FireRateMod.ToMod(this));
            stats.MaxHealth.AddModifier(db.GunslingerParams.HealthLooseMod.ToMod(this));
            stats.ShotRange.AddModifier(db.GunslingerParams.ShotRangeMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.FireRate.RemoveAllModifiersFromSource(this);
            stats.MaxHealth.RemoveAllModifiersFromSource(this);
            stats.ShotRange.RemoveAllModifiersFromSource(this);
        }
    }
}