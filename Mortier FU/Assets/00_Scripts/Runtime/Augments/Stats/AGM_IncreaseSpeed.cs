namespace MortierFu.Stats
{
    public class AGM_IncreaseSpeed : AugmentBase
    {
        public AGM_IncreaseSpeed(DA_Augment augmentData, PlayerCharacter owner) : base(augmentData, owner)
        { }

        public override void Initialize()
        {
            stats.MoveSpeed.AddModifier(new StatModifier(0.3f, StatModType.PercentMult, this));
        }
        
        public override void DeInitialize()
        {
            stats.MoveSpeed.RemoveAllModifiersFromSource(this);
        }
    }
}