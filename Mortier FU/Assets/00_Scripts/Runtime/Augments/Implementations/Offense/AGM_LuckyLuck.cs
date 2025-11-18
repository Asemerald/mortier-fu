namespace MortierFu.Stats
{
    public class AGM_LuckyLuck : AugmentBase
    {
        public AGM_LuckyLuck(SO_Augment augmentData, PlayerCharacter owner) : base(augmentData, owner)
        { }

        public override void Initialize()
        {
            stats.FireRate.AddModifier(new StatModifier(-0.3f, E_StatModType.PercentAdd, this));
            // TODO Trouver une solution plus propre pour éviter d'hardset
            stats.MaxHealth.BaseValue = 2;
        }
        
        public override void Dispose()
        {
            stats.FireRate.RemoveAllModifiersFromSource(this);
            // TODO Trouver une solution plus propre pour éviter d'hardset
            stats.MaxHealth.BaseValue = 4;
        }
    }
}