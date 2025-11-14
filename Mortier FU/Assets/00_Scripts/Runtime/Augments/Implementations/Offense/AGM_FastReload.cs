namespace MortierFu.Stats
{
    public class AGM_FastReload : AugmentBase
    {
        public AGM_FastReload(SO_Augment augmentData, PlayerCharacter owner) : base(augmentData, owner)
        { }

        public override void Initialize()
        {
            stats.FireRate.AddModifier(new StatModifier(-0.15f, E_StatModType.PercentMult, this));
        }
        
        public override void Dispose()
        {
            stats.FireRate.RemoveAllModifiersFromSource(this);
        }
    }
}