namespace MortierFu
{
    public class AGM_Ballista : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod FireRateMod;
            public AugmentStatMod BombshellDamageMod;
        }
        
        public AGM_Ballista(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }
        
        public override void Initialize()
        {
            stats.FireRate.AddModifier(db.BallistaParams.FireRateMod.ToMod(this));
            stats.BombshellDamage.AddModifier(db.BallistaParams.BombshellDamageMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.FireRate.RemoveAllModifiersFromSource(this);
            stats.BombshellDamage.RemoveAllModifiersFromSource(this);
        }
    }
}