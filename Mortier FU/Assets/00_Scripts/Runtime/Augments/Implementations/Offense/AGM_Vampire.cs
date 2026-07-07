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

        public AGM_Vampire(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.BombshellDamage.AddModifier(db.VampireParams.BombshellDamageMod.ToMod(this));

            _hitBinding = new EventBinding<TriggerHit>(OnHit);
            EventBus<TriggerHit>.Register(_hitBinding);
        }

        private void OnHit(TriggerHit evt)
        {
            if (!CanProc(evt))
                return;

            bool healed = owner.Health.Heal(db.VampireParams.MaxHealthMod.Value, owner);

            if (healed)
                AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Augment_Buff, owner.transform.position);
        }

        private bool CanProc(TriggerHit evt)
        {
            if (!owner || owner.Health is not { IsAlive: true })
                return false;

            if (owner.Owner != null && owner.Owner.IsControllingGhost)
                return false;

            if (!evt.Bombshell || evt.Bombshell.Owner != owner)
                return false;

            if (evt.HitCharacters == null || evt.HitCharacters.Length == 0)
                return false;

            return owner.ControlContext == PlayerControlContext.RoundGameplay;
        }

        public override void Dispose()
        {
            if (_hitBinding != null)
                EventBus<TriggerHit>.Deregister(_hitBinding);

            stats.BombshellDamage.RemoveAllModifiersFromSource(this);
        }
    }
}