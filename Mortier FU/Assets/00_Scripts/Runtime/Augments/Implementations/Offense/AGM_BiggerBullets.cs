namespace MortierFu.Stats
{
    public class AGM_BiggerBullets : AugmentBase
    {
        public AGM_BiggerBullets(SO_Augment augmentData, PlayerCharacter owner) : base(augmentData, owner)
        { }

        public override void Initialize()
        {
            stats.DamageAmount.AddModifier(new StatModifier(1, E_StatModType.Flat, this));
        }
        
        public override void Dispose()
        {
            stats.DamageAmount.RemoveAllModifiersFromSource(this);
        }
    }
}