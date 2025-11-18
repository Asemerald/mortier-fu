namespace MortierFu.Stats
{
    public class AGM_Ballista : AugmentBase
    {
        public AGM_Ballista(SO_Augment augmentData, PlayerCharacter owner) : base(augmentData, owner)
        { }
        
        public override void Initialize()
        {
            stats.FireRate.AddModifier(new StatModifier(-0.8f, E_StatModType.PercentAdd, this));
            stats.BombshellDamage.AddModifier(new StatModifier(-2.0f, E_StatModType.Flat, this));
        }
        
        public override void Dispose()
        {
            stats.FireRate.RemoveAllModifiersFromSource(this);
            stats.BombshellDamage.RemoveAllModifiersFromSource(this);
        }
    }
}