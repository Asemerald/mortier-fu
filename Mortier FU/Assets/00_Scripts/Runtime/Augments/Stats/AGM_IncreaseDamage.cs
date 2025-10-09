namespace MortierFu.Stats
{
    public class AGM_IncreaseDamage : AugmentBase
    {
        public AGM_IncreaseDamage(DA_Augment augmentData, Character owner) : base(augmentData, owner)
        { }

        public override void Initialize()
        {
            stats.Damage.AddModifier(new(10.0f, StatModType.Flat, this));
        }

        public override void DeInitialize()
        {
            stats.Damage.RemoveAllModifiersFromSource(this);
        }
    }
}