namespace MortierFu
{
    public class AGM_BouncyPrototype : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod BombShellDamageMod;
            public int ExtraBombshellBounces;
        }
        
        public AGM_BouncyPrototype(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.BombshellDamage.AddModifier(db.BouncyPrototypeParams.BombShellDamageMod.ToMod(this));
            stats.BombshellBounces.AddModifier(new StatModifier(db.BouncyPrototypeParams.ExtraBombshellBounces, E_StatModType.Flat, this));
        }

        public override void Dispose()
        {
            stats.FireRate.RemoveAllModifiersFromSource(this);
            stats.BombshellBounces.RemoveAllModifiersFromSource(this);
        }
    }
}