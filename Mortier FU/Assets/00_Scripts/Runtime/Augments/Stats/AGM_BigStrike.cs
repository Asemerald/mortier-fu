namespace MortierFu.Stats
{
    public class AGM_BigStrike : AugmentBase
    {
        public AGM_BigStrike(DA_Augment augmentData, PlayerCharacter owner) : base(augmentData, owner)
        { }

        public override void Initialize()
        {
            stats.StrikeStunDuration.AddModifier(new StatModifier(0.2f, StatModType.PercentMult, this));
            stats.StrikeCooldown.AddModifier(new StatModifier(0.5f, StatModType.PercentMult, this));
        }
        
        public override void DeInitialize()
        {
            stats.StrikeStunDuration.RemoveAllModifiersFromSource(this);
            stats.StrikeCooldown.RemoveAllModifiersFromSource(this);
        }
    }
}