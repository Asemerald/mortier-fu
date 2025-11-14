namespace MortierFu
{
    public class AGM_Berserker : AugmentHealthThresholdBase
    {
        public AGM_Berserker(SO_Augment augmentData, PlayerCharacter owner) : base(augmentData, owner)
        { }

        protected override float HealthThreshold => 0.5f;

        protected override void OnEnterThreshold()
        {
            stats.BombshellDamage.AddModifier(new StatModifier(2f, E_StatModType.Flat, this));
        }

        protected override void OnExitThreshold()
        {
            stats.BombshellDamage.RemoveAllModifiersFromSource(this);
        }

        public override void Dispose()
        {
            if(IsActive) OnExitThreshold();
            base.Dispose();
        }
    }
}