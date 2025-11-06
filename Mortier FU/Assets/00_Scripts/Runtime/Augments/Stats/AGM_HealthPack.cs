namespace MortierFu.Stats
{
    public class AGM_HealthPack : AugmentBase
    {
        public AGM_HealthPack(SO_Augment augmentData, PlayerCharacter owner) : base(augmentData, owner)
        { }

        public override void Initialize()
        {
            stats.MaxHealth.AddModifier(new StatModifier(1.0f, StatModType.Flat, this));
        }
        
        public override void DeInitialize()
        {
            stats.MaxHealth.RemoveAllModifiersFromSource(this);
        }
    }
}