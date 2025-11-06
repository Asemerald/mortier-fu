namespace MortierFu.Stats
{
    public class AGM_FastReload : AugmentBase
    {
        public AGM_FastReload(SO_Augment augmentData, PlayerCharacter owner) : base(augmentData, owner)
        { }

        public override void Initialize()
        {
            stats.FireRate.AddModifier(new StatModifier(-0.15f, StatModType.PercentMult, this));
        }
        
        public override void DeInitialize()
        {
            stats.FireRate.RemoveAllModifiersFromSource(this);
        }
    }
}