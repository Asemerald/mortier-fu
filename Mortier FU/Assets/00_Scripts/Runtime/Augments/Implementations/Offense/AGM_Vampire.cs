using UnityEngine.Serialization;

namespace MortierFu
{
    public class AGM_Vampire : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public AugmentStatMod BombshellDamageMod;
            [FormerlySerializedAs("AmountHealMod")] public AugmentStatMod MaxHealthMod;
        }

        private EventBinding<TriggerHit> _hitBinding;

        public AGM_Vampire(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData,
            owner, db)
        { }

        public override void Initialize()
        {
            stats.BombshellDamage.AddModifier(db.VampireParams.BombshellDamageMod.ToMod(this));

            _hitBinding = new EventBinding<TriggerHit>(OnHit);
            EventBus<TriggerHit>.Register(_hitBinding);
        }

        private void OnHit(TriggerHit evt)
        {
            if (evt.Bombshell.Owner != owner) return;

            evt.Bombshell.Owner.Health.Heal(db.VampireParams.MaxHealthMod.Value);
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Augment_Buff, owner.transform.position);
        }

        public override void Dispose()
        {
            EventBus<TriggerHit>.Deregister(_hitBinding);
            stats.BombshellImpactRadius.RemoveAllModifiersFromSource(this);
        }
    }
}