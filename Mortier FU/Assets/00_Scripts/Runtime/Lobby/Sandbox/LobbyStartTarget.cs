using System;
using UnityEngine;
using UnityEngine.Playables;

namespace MortierFu
{
    public sealed class LobbyStartTarget : MonoBehaviour
    {
        public event Action<PlayerManager> OnHitByPlayer;

        [Header("Animation")]
        [SerializeField] private PlayableDirector _dongAnimation;

        [Header("References")]
        [SerializeField] private LobbyStartReadyController _lobbyStartReadyController;

        private EventBinding<TriggerBombshellImpact> _bombshellImpactBinding;

        private void Awake()
        {
            if (!_lobbyStartReadyController)
                _lobbyStartReadyController = GetComponent<LobbyStartReadyController>();

            _bombshellImpactBinding = new EventBinding<TriggerBombshellImpact>(OnBombshellImpact);
        }

        private void OnEnable() => EventBus<TriggerBombshellImpact>.Register(_bombshellImpactBinding);

        private void OnDisable() => EventBus<TriggerBombshellImpact>.Deregister(_bombshellImpactBinding);

        private void OnDestroy() => OnHitByPlayer = null;

        private void OnBombshellImpact(TriggerBombshellImpact evt)
        {
            if (!IsTargetHit(evt.HitObject))
                return;

            Bombshell bombshell = evt.Bombshell;

            if (!bombshell)
                return;

            PlayerCharacter shooterCharacter = bombshell.Owner;

            if (!shooterCharacter || !shooterCharacter.Owner)
                return;

            OnHitByPlayer?.Invoke(shooterCharacter.Owner);
        }

        private bool IsTargetHit(GameObject hitObject)
        {
            if (!hitObject)
                return false;

            return hitObject == gameObject || hitObject.transform.IsChildOf(transform);
        }

        public void PlayDongAnimation()
        {
            if (!_dongAnimation)
                return;

            _dongAnimation.Stop();
            _dongAnimation.time = 0f;
            _dongAnimation.Evaluate();
            _dongAnimation.Play();
        }

        public void ConfirmAnimationEnd()
        {
            if (_lobbyStartReadyController)
                _lobbyStartReadyController.StartMatch();
        }
    }
}