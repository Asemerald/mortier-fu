namespace MortierFu.Stats
{
    public class AGM_LuckyLuck : AugmentBase
    {
        public AGM_LuckyLuck(DA_Augment augmentData, PlayerCharacter owner) : base(augmentData, owner)
        { }

        public override void Initialize()
        {
            stats.FireRate.AddModifier(new StatModifier(-0.3f, StatModType.PercentMult, this));
            // TODO Trouver une solution plus propre pour éviter d'hardset
            stats.MaxHealth.BaseValue = 2;
        }
        
        public override void DeInitialize()
        {
            stats.FireRate.RemoveAllModifiersFromSource(this);
            // TODO Trouver une solution plus propre pour éviter d'hardset
            stats.MaxHealth.BaseValue = 4;
        }
    }
}