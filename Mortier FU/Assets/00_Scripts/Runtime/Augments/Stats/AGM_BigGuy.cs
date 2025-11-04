namespace MortierFu.Stats
{
    public class AGM_BigGuy : AugmentBase
    {
        public AGM_BigGuy(DA_Augment augmentData, PlayerCharacter owner) : base(augmentData, owner)
        { }

        public override void Initialize()
        {
            stats.MaxHealth.AddModifier(new StatModifier(3, StatModType.Flat, this));
            stats.FireRate.AddModifier(new StatModifier(0.5f, StatModType.PercentMult, this));
        }
        
        public override void DeInitialize()
        {
            stats.MaxHealth.RemoveAllModifiersFromSource(this);
            stats.FireRate.RemoveAllModifiersFromSource(this);
        }
    }
}