using System;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbyStartTarget : MonoBehaviour
    {
        public event Action<PlayerManager> OnHitByPlayer;

        private EventBinding<TriggerBombshellImpact> _bombshellImpactBinding;

        private void Awake()
        {
            _bombshellImpactBinding = new EventBinding<TriggerBombshellImpact>(OnBombshellImpact);
        }

        private void OnEnable()
        {
            EventBus<TriggerBombshellImpact>.Register(_bombshellImpactBinding);
        }

        private void OnDisable()
        {
            EventBus<TriggerBombshellImpact>.Deregister(_bombshellImpactBinding);
        }

        private void OnDestroy()
        {
            OnHitByPlayer = null;
        }

        private void OnBombshellImpact(TriggerBombshellImpact evt)
        {
            if (!IsTargetHit(evt.HitObject))
                return;

            var bombshell = evt.Bombshell;

            if (!bombshell)
                return;

            var shooterCharacter = bombshell.Owner;

            if (!shooterCharacter || !shooterCharacter.Owner)
                return;

            OnHitByPlayer?.Invoke(shooterCharacter.Owner);
        }

        private bool IsTargetHit(GameObject hitObject)
        {
            if (!hitObject)
                return false;

            if (hitObject == gameObject)
                return true;

            return hitObject.transform.IsChildOf(transform);
        }
    }
}