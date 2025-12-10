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
        private AugmentSelectionSystem _augmentSelectionSystem;

        private Tween _vignetteTween;
        private ConfirmationService _confirmationService;

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

            _augmentSelectionSystem = SystemManager.Instance.Get<AugmentSelectionSystem>();
            if (_augmentSelectionSystem != null)
            {
                _augmentSelectionSystem.OnPressureStart += StartVignettePressure;
                _augmentSelectionSystem.OnPressureStop += StopVignettePressure;
            }
            else
            {
                Debug.LogError($"[PlayerConfirmationUI] No AugmentSelectionSystem found for {gameObject.name}");
            }

            _activePlayerCount = _lobbyService.GetPlayers().Count;

            _confirmationService = ServiceManager.Instance.Get<ConfirmationService>();
            if (_confirmationService != null)
            {
                _confirmationService.OnPlayerConfirmed += NotifyPlayerConfirmed;
                _confirmationService.OnStartConfirmation += ShowConfirmation;
                _confirmationService.OnAllPlayersConfirmed += OnConfirmation;
            }
            else
            {
                Debug.LogError($"[PlayerConfirmationUI] No ConfirmationService found for {gameObject.name}");
            }
        }

        private void OnDestroy()
        {
            _augmentSelectionSystem.OnPressureStart -= StartVignettePressure;
            _augmentSelectionSystem.OnPressureStop -= StopVignettePressure;

            _confirmationService.OnPlayerConfirmed -= NotifyPlayerConfirmed;
            _confirmationService.OnStartConfirmation -= ShowConfirmation;
            _confirmationService.OnAllPlayersConfirmed -= OnConfirmation;
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

        private void StartVignettePressure(float duration)
        {
            if (_vignetteImage == null)
                return;

            _vignetteImage.gameObject.SetActive(true);
            if (_vignetteTween.isAlive)
                _vignetteTween.Stop();

            var color = _vignetteImage.color;
            color.a = 0f;
            _vignetteImage.color = color;

            _vignetteTween = Tween.Custom(0f, 1f, 0.6f,
                a =>
                {
                    var c = _vignetteImage.color;
                    c.a = a;
                    _vignetteImage.color = c;
                },
                cycles: -1,
                cycleMode: CycleMode.Yoyo
            );

            Tween.Delay(duration, StopVignettePressure);
        }

        private void StopVignettePressure()
        {
            if (_vignetteTween.isAlive)
                _vignetteTween.Stop();

            if (_vignetteImage != null)
                _vignetteImage.gameObject.SetActive(false);
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