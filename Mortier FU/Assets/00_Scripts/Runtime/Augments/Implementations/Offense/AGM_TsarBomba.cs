namespace MortierFu.Stats
{
    public class AGM_TsarBomba : AugmentBase
    {
        public AGM_TsarBomba(SO_Augment augmentData, PlayerCharacter owner) : base(augmentData, owner)
        { }

        public override void Initialize()
        {
            stats.BombshellImpactRadius.AddModifier(new StatModifier(3f, E_StatModType.PercentAdd, this));
            stats.BombshellDamage.AddModifier(new StatModifier(1f, E_StatModType.Flat, this));
            stats.FireRate.AddModifier(new StatModifier(1.5f, E_StatModType.PercentAdd, this));
            stats.BombshellTimeTravel.AddModifier(new StatModifier(1.5f, E_StatModType.PercentAdd, this));
        }
        
        public override void Dispose()
        {
            stats.BombshellImpactRadius.RemoveAllModifiersFromSource(this);
            stats.BombshellDamage.RemoveAllModifiersFromSource(this);
            stats.FireRate.RemoveAllModifiersFromSource(this);
            stats.BombshellTimeTravel.RemoveAllModifiersFromSource(this);
        }
    }
}