namespace MortierFu
{
    public class AGM_Confidence : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod BombshellDamageMod;
            public AugmentStatMod BombshellTimeTravelMod;
        }
        
        public AGM_Confidence(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }
        
        public override void Initialize()
        {
            stats.BombshellDamage.AddModifier(db.ConfidenceParams.BombshellDamageMod.ToMod(this));
            stats.BombshellTimeTravel.AddModifier(db.ConfidenceParams.BombshellTimeTravelMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.BombshellDamage.RemoveAllModifiersFromSource(this);
            stats.BombshellTimeTravel.RemoveAllModifiersFromSource(this);
        }
    }
}