namespace MortierFu
{
    public class AGM_FastStrike : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod StrikeCooldownMod;
        }
        
        public AGM_FastStrike(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.StrikeCooldown.AddModifier(db.FastStrikeParams.StrikeCooldownMod.ToMod(this));
        }
        
        public override void Dispose()
        {
            stats.StrikeCooldown.RemoveAllModifiersFromSource(this);
        }
    }
}