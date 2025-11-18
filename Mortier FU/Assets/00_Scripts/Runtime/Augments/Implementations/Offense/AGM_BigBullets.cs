namespace MortierFu
{
    public class AGM_BigBullets : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod FireRateMod;
            public AugmentStatMod BombshellDamageMod;
            public AugmentStatMod BombshellImpactRadiusMod;
        }
        
        public AGM_BigBullets(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }
        
        public override void Initialize()
        {
            stats.FireRate.AddModifier(db.BigBulletsParams.FireRateMod.ToMod(this));
            stats.BombshellDamage.AddModifier(db.BigBulletsParams.BombshellDamageMod.ToMod(this));
            stats.BombshellImpactRadius.AddModifier(db.BigBulletsParams.BombshellImpactRadiusMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.FireRate.RemoveAllModifiersFromSource(this);
            stats.BombshellDamage.RemoveAllModifiersFromSource(this);
            stats.BombshellImpactRadius.RemoveAllModifiersFromSource(this);
        }
    }
}