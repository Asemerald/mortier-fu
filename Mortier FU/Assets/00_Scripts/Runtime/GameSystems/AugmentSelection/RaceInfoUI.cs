using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;
using PrimeTween;

namespace MortierFu
{
    public class RaceInfoUI : MonoBehaviour
    {
        [Header("Player Slots (Blue, Green, Red, Yellow order)")] [SerializeField]
        private List<PlayerSlot> _playerSlots;

        [Header("Animation Settings")] [SerializeField]
        private float _pulseScale = 1.15f;

        [SerializeField] private float _pulseDuration = 0.45f;

        [Header("References")] [SerializeField]
        private GameObject _horizontalLayoutParent;

        [SerializeField] private Image _vignetteImage;

        private int _activePlayerCount;

        private LobbyService _lobbyService;

        private void Awake()
        {
            if (_horizontalLayoutParent != null)
                _horizontalLayoutParent.SetActive(false);
        }

        private void Start()
        {
            _lobbyService = ServiceManager.Instance.Get<LobbyService>();
            if (_lobbyService == null)
            {
                Debug.LogError($"[PlayerConfirmationUI] No LobbyService found for {gameObject.name}");
                return;
            }

            _activePlayerCount = _lobbyService.GetPlayers().Count;

            var service = ServiceManager.Instance.Get<ConfirmationService>();
            if (service != null)
            {
                service.OnPlayerConfirmed += NotifyPlayerConfirmed;
                service.OnStartConfirmation += ShowConfirmation;
                service.OnAllPlayersConfirmed += OnConfirmation;
            }
            else
            {
                Debug.LogError($"[PlayerConfirmationUI] No ConfirmationService found for {gameObject.name}");
            }
        }

        private void OnDestroy()
        {
            var service = ServiceManager.Instance.Get<ConfirmationService>();
            if (service == null) return;

            service.OnPlayerConfirmed -= NotifyPlayerConfirmed;
            service.OnStartConfirmation -= ShowConfirmation;
            service.OnAllPlayersConfirmed -= OnConfirmation;
        }

        private void ShowConfirmation()
        {
            if (_horizontalLayoutParent != null)
            {
                _horizontalLayoutParent.SetActive(true);
            }

            StartAllAnimations(_activePlayerCount);
        }

        private void OnConfirmation()
        {
            HideConfirmation().Forget();
        }

        private async UniTaskVoid HideConfirmation()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.25f));
            
            foreach (var slot in _playerSlots)
            {
                if (!slot.IsActive) continue;

                if (slot.ATween.isAlive)
                    slot.ATween.Stop();
                if (slot.ScaleTween.isAlive)
                    slot.ScaleTween.Complete();

                var target = slot.Animator.gameObject.transform;

                slot.ScaleTween = Tween.Scale(target, Vector3.one, Vector3.zero, 0.6f, Ease.InQuint).OnComplete(() =>
                {
                    slot.Animator.enabled = false;
                });
            }

            await UniTask.Delay(TimeSpan.FromSeconds(0.6f));

            if (_horizontalLayoutParent != null)
                _horizontalLayoutParent.SetActive(false);
        }

        private void StartAllAnimations(int playerCount)
        {
            for (int i = 0; i < _activePlayerCount; i++)
            {
                _playerSlots[i].Animator.gameObject.SetActive(true);
            }

            for (int i = 0; i < _playerSlots.Count; i++)
            {
                var slot = _playerSlots[i];
                slot.IsActive = i < playerCount;

                slot.AButtonImage.gameObject.SetActive(slot.IsActive);
                slot.OkImage.gameObject.SetActive(false);

                if (slot.ATween.isAlive)
                    slot.ATween.Stop();
                if (slot.ScaleTween.isAlive)
                    slot.ScaleTween.Complete();

                if (!slot.IsActive) continue;

                var target = slot.Animator.gameObject.transform;

                slot.ScaleTween = Tween.Scale(target, Vector3.zero, Vector3.one, 0.5f, Ease.OutBack);

                slot.ATween = Tween.Scale(
                    target: slot.AButtonImage.rectTransform,
                    Vector3.one * _pulseScale,
                    duration: _pulseDuration,
                    ease: Ease.InOutQuad,
                    cycles: -1,
                    cycleMode: CycleMode.Yoyo
                );
            }
        }

        private void NotifyPlayerConfirmed(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= _playerSlots.Count)
                return;

            var slot = _playerSlots[playerIndex];

            if (slot.ATween.isAlive)
                slot.ATween.Stop();

            slot.AButtonImage.gameObject.SetActive(false);
            slot.Animator.enabled = true;
        }

        [Serializable]
        public class PlayerSlot
        {
            public Image AButtonImage;
            public Image OkImage;
            public Animator Animator;
            public bool IsActive;
            [HideInInspector] public Tween ATween;
            [HideInInspector] public Tween ScaleTween;
        }
    }
}