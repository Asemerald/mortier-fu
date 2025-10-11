namespace CustomMod
{
    public class CustomAugment : MortierFu.AugmentBase
    {
        public CustomAugment(MortierFu.DA_Augment augmentData, MortierFu.Character owner) : base(augmentData, owner)
        { }

        public override void Initialize()
        {
            // Example: Increase damage by 15%
            stats.Damage.AddModifier(new MortierFu.StatModifier(0.15f, MortierFu.StatModType.PercentMult, this));
        }

        public override void DeInitialize()
        {
            stats.Damage.RemoveAllModifiersFromSource(this);
        }
    }
}