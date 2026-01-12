namespace MortierFu
{
    public class AGM_Confidence : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod BombshellDamageMod;
            public AugmentStatMod BombshellSpeedMod;
        }
        
        public AGM_Confidence(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }
        
        public override void Initialize()
        {
            stats.BombshellDamage.AddModifier(db.ConfidenceParams.BombshellDamageMod.ToMod(this));
            stats.BombshellSpeed.AddModifier(db.ConfidenceParams.BombshellSpeedMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.BombshellDamage.RemoveAllModifiersFromSource(this);
            stats.BombshellSpeed.RemoveAllModifiersFromSource(this);
        }
    }
}