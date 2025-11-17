namespace MortierFu.Stats
{
    public class AGM_SharperBullets : AugmentBase
    {
        public AGM_SharperBullets(SO_Augment augmentData, PlayerCharacter owner) : base(augmentData, owner)
        { }

        public override void Initialize()
        {
            stats.BombshellDamage.AddModifier(new StatModifier(1, E_StatModType.Flat, this));
        }
        
        public override void Dispose()
        {
            stats.BombshellDamage.RemoveAllModifiersFromSource(this);
        }
    }
}