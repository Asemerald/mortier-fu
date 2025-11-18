namespace MortierFu.Stats
{
    public class AGM_MaximumVelocity : AugmentBase
    {
        public AGM_MaximumVelocity(SO_Augment augmentData, PlayerCharacter owner) : base(augmentData, owner)
        { }

        public override void Initialize()
        {
            stats.BombshellTimeTravel.AddModifier(new StatModifier(-0.5f, E_StatModType.PercentAdd, this));
            stats.FireRate.AddModifier(new StatModifier(-0.1f, E_StatModType.PercentAdd, this));
        }
            
        public override void Dispose()
        {
            stats.BombshellTimeTravel.RemoveAllModifiersFromSource(this);
            stats.FireRate.RemoveAllModifiersFromSource(this);
        }
    }
}