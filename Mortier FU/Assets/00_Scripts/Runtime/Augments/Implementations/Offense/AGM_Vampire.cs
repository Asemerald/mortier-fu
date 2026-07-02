namespace MortierFu
{
    public class AGM_Vampire : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod BombshellImpactRadiusMod;
            public AugmentStatMod AmountHealMod;
        }

        private EventBinding<TriggerHit> _hitBinding;

        public AGM_Vampire(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData,
            owner, db)
        { }

        public override void Initialize()
        {
            stats.BombshellImpactRadius.AddModifier(db.VampireParams.BombshellImpactRadiusMod.ToMod(this));

            _hitBinding = new EventBinding<TriggerHit>(OnHit);
            EventBus<TriggerHit>.Register(_hitBinding);
        }

        private void OnHit(TriggerHit evt)
        {
            if (evt.Bombshell.Owner != owner) return;

            evt.Bombshell.Owner.Health.Heal(db.VampireParams.AmountHealMod.Value);
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Augment_Buff, owner.transform.position);
        }

        public override void Dispose()
        {
            EventBus<TriggerHit>.Deregister(_hitBinding);
            stats.BombshellImpactRadius.RemoveAllModifiersFromSource(this);
        }
    }
}