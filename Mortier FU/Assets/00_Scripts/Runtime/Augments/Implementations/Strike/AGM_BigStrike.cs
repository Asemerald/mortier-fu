namespace MortierFu
{
    public class AGM_BigStrike : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod StrikePushForceMod;
            public AugmentStatMod StrikeCooldownMod;
        }
        
        public AGM_BigStrike(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.StrikePushForce.AddModifier(db.BigStrikeParams.StrikePushForceMod.ToMod(this));
            stats.StrikeCooldown.AddModifier(db.BigStrikeParams.StrikeCooldownMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.StrikePushForce.RemoveAllModifiersFromSource(this);
            stats.StrikeCooldown.RemoveAllModifiersFromSource(this);
        }
    }
}