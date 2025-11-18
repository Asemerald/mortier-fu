namespace MortierFu
{
    public class AGM_TsarBomba : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod BombshellImpactRadius;
            public AugmentStatMod BombshellDamageMod;
            public AugmentStatMod FireRateMod;
            public AugmentStatMod BombshellTimeTravelMod;
        }
        
        public AGM_TsarBomba(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.BombshellImpactRadius.AddModifier(db.TsarBombaParams.BombshellImpactRadius.ToMod(this));
            stats.BombshellDamage.AddModifier(db.TsarBombaParams.BombshellDamageMod.ToMod(this));
            stats.FireRate.AddModifier(db.TsarBombaParams.FireRateMod.ToMod(this));
            stats.BombshellTimeTravel.AddModifier(db.TsarBombaParams.BombshellTimeTravelMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.BombshellImpactRadius.RemoveAllModifiersFromSource(this);
            stats.BombshellDamage.RemoveAllModifiersFromSource(this);
            stats.FireRate.RemoveAllModifiersFromSource(this);
            stats.BombshellTimeTravel.RemoveAllModifiersFromSource(this);
        }
    }
}