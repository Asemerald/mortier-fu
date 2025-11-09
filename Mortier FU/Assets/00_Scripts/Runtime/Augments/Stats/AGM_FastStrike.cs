namespace MortierFu.Stats
{
    public class AGM_FastStrike : AugmentBase
    {
        public AGM_FastStrike(SO_Augment augmentData, PlayerCharacter owner) : base(augmentData, owner)
        { }

        public override void Initialize()
        {
            stats.StrikeCooldown.AddModifier(new StatModifier(-0.15f, E_StatModType.PercentMult, this));
        }
        
        public override void DeInitialize()
        {
            stats.StrikeCooldown.RemoveAllModifiersFromSource(this);
        }
    }
}