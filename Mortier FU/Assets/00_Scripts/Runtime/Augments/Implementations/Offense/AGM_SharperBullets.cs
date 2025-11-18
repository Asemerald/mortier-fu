namespace MortierFu
{
    public class AGM_SharperBullets : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod BombshellDamageMod;
        }
        
        public AGM_SharperBullets(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.BombshellDamage.AddModifier(db.SharperBulletsParams.BombshellDamageMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.BombshellDamage.RemoveAllModifiersFromSource(this);
        }
    }
}