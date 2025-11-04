namespace MortierFu.Stats
{
    public class AGM_BiggerBullets : AugmentBase
    {
        public AGM_BiggerBullets(DA_Augment augmentData, PlayerCharacter owner) : base(augmentData, owner)
        { }

        public override void Initialize()
        {
            stats.DamageAmount.AddModifier(new StatModifier(1, StatModType.Flat, this));
        }
        
        public override void DeInitialize()
        {
            stats.DamageAmount.RemoveAllModifiersFromSource(this);
        }
    }
}