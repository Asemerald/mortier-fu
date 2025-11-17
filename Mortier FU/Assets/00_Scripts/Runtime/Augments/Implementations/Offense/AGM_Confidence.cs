namespace MortierFu.Stats
{
    public class AGM_Confidence : AugmentBase
    {
        public AGM_Confidence(SO_Augment augmentData, PlayerCharacter owner) : base(augmentData, owner)
        { }
        
        public override void Initialize()
        {
            stats.BombshellDamage.AddModifier(new StatModifier(2f, E_StatModType.Flat, this));
            stats.BombshellTimeTravel.AddModifier(new StatModifier(0.5f, E_StatModType.PercentMult, this));
        }
        
        public override void Dispose()
        {
            stats.BombshellDamage.RemoveAllModifiersFromSource(this);
            stats.BombshellTimeTravel.RemoveAllModifiersFromSource(this);
        }
    }
}