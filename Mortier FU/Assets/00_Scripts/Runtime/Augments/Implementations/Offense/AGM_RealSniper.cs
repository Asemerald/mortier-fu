namespace MortierFu
{
    public class AGM_RealSniper : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod ShotRangeMod;
            public AugmentStatMod BombshellSpeedMod;
            public AugmentStatMod FireRateMod;
            public AugmentStatMod BombshellDamageMod;
            public AugmentStatMod ImpactRadiusMod;
        }
        
        public AGM_RealSniper(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.BombshellDamage.AddModifier(db.RealSniperParams.BombshellDamageMod.ToMod(this));
            stats.BombshellSpeed.AddModifier(db.RealSniperParams.BombshellSpeedMod.ToMod(this));
            stats.ShotRange.AddModifier(db.RealSniperParams.ShotRangeMod.ToMod(this));
            stats.FireRate.AddModifier(db.RealSniperParams.FireRateMod.ToMod(this));
            stats.BombshellImpactRadius.AddModifier(db.RealSniperParams.ImpactRadiusMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.BombshellDamage.RemoveAllModifiersFromSource(this);
            stats.BombshellSpeed.RemoveAllModifiersFromSource(this);
            stats.FireRate.RemoveAllModifiersFromSource(this);
            stats.ShotRange.RemoveAllModifiersFromSource(this);
            stats.BombshellImpactRadius.RemoveAllModifiersFromSource(this);
        }
    }
}