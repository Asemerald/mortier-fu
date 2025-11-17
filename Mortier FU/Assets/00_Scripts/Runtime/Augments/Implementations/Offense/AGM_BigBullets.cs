namespace MortierFu.Stats
{
    public class AGM_BigBullets : AugmentBase
    {
        public AGM_BigBullets(SO_Augment augmentData, PlayerCharacter owner) : base(augmentData, owner)
        { }
        
        public override void Initialize()
        {
            stats.FireRate.AddModifier(new StatModifier(0.4f, E_StatModType.PercentMult, this));
            stats.BombshellDamage.AddModifier(new StatModifier(1f, E_StatModType.Flat, this));
            stats.DamageRange.AddModifier(new StatModifier(0.5f, E_StatModType.PercentMult, this));
        }
        
        public override void Dispose()
        {
            stats.FireRate.RemoveAllModifiersFromSource(this);
            stats.BombshellDamage.RemoveAllModifiersFromSource(this);
            stats.DamageRange.RemoveAllModifiersFromSource(this);
        }
    }
}