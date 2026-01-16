using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

namespace MortierFu
{
    public class PlayerConfirmationUI : MonoBehaviour
    {
        #region Variables

        #region Player Slots

        [Header("Player Slots (Blue, Red, Green, Yellow)")] [SerializeField]
        private List<PlayerSlot> _playerSlots;

        #endregion

        #region UI References

        [Header("UI References")] [SerializeField]
        private Image _countdownImage;

        [SerializeField] private GameObject _raceGameObject;

        #endregion

        #region Assets

        [Header("Assets")] [SerializeField] private List<Sprite> _countdownSprites;

        #endregion

        #region General Animation Settings

        [Header("General Animation Settings")] [SerializeField]
        private float _pulseScale = 1.15f;

        [SerializeField] private float _pulseDuration = 0.45f;
        [SerializeField] private float _hideDuration = 0.6f;
        [SerializeField] private float _defaultScaleDuration = 0.5f;

        #endregion

        #region Race Animation Settings

        [Header("Race Animation Settings")] [SerializeField]
        private float _racePopDuration = 0.5f;

        [SerializeField] private float _raceDisableDelay = 0.5f;

        #endregion

        #region Ease Settings

        [Header("Ease Settings")] [SerializeField]
        private Ease _actionButtonEaseOut = Ease.OutBack;

        [SerializeField] private Ease _actionImageEaseInOut = Ease.InOutQuad;
        [SerializeField] private Ease _playEaseIn = Ease.InBack;
        [SerializeField] private Ease _slotEaseIn = Ease.InQuint;

        #endregion

        #region Runtime State

        private Vector3 _initialRaceScale;
        private Vector3 _initialCountdownScale;

        private Sequence _countdownSequence;

        private CancellationTokenSource _cts;

        #endregion

        #endregion

        private void Awake()
        {
            _initialCountdownScale = _countdownImage.transform.localScale;
            _initialRaceScale = _raceGameObject.transform.localScale;

            _raceGameObject.SetActive(false);
            _countdownImage.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
        }

        private void OnDisable()
        {
            CleanupTweens();
            _cts?.Cancel();
        }

        private void OnDestroy()
        {
            CleanupTweens();
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private void CleanupTweens()
        {
            if (_countdownSequence.isAlive)
                _countdownSequence.Stop();

            foreach (var slot in _playerSlots)
            {
                slot.ATween.Stop();
                slot.ScaleTween.Stop();
            }
        }

        private async UniTask PlayCountdown(int seconds = 3)
        {
            ShowCountdownImage();
            _countdownImage.gameObject.SetActive(true);
            _countdownImage.sprite = _countdownSprites[0];

            for (int t = seconds; t > 0; t--)
            {
                SetCountdownVisual(t);
                // TODO: Add sound effect here or maybe in AnimateCountdownNumber
                await AnimateCountdownNumber(_cts.Token);
            }

            _countdownImage.gameObject.SetActive(false);

            await ShowPlay(_cts.Token);
            
            // TODO: Désolé c'est horrible
            var gm = GameService.CurrentGameMode as GameModeBase;
            gm?.EnablePlayerInputs();
        }

        private async UniTask ShowPlay(CancellationToken ct)
        {
            _raceGameObject.SetActive(true);
            _raceGameObject.transform.localScale = Vector3.zero;

            await Tween.Scale(
                _raceGameObject.transform,
                Vector3.zero,
                _initialRaceScale,
                _racePopDuration,
                _playEaseIn
            ).ToUniTask(cancellationToken: ct);

            await UniTask.Delay(TimeSpan.FromSeconds(_raceDisableDelay), cancellationToken: ct);

            _raceGameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        private void SetCountdownVisual(int number)
        {
            int index = Mathf.Clamp(_countdownSprites.Count - number, 0, _countdownSprites.Count - 1);
            _countdownImage.sprite = _countdownSprites[index];
        }

        private async UniTask AnimateCountdownNumber(CancellationToken ct)
        {
            if (_countdownSequence.isAlive)
                _countdownSequence.Stop();

            _countdownImage.transform.localScale = Vector3.zero;

            const float growthDuration = 0.3f;
            const float shrinkDuration = 0.3f;
            const float bumpDuration = 0.4f;

            _countdownSequence = Sequence.Create()
                .Chain(Tween.Scale(_countdownImage.transform, Vector3.zero, Vector3.one, growthDuration, Ease.OutBack))
                .Group(Tween.Rotation(
                    _countdownImage.transform,
                    Quaternion.Euler(0f, 0f, 180),
                    Quaternion.Euler(0f, 0f, 0f),
                    growthDuration * 0.9f,
                    Ease.OutBack,
                    startDelay: growthDuration * 0.1f
                ))
                .ChainDelay(bumpDuration)
                .Chain(Tween.Scale(_countdownImage.transform, Vector3.zero, shrinkDuration, Ease.InBack))
                .Group(Tween.Rotation(
                    _countdownImage.transform,
                    Quaternion.Euler(0f, 0f, 180),
                    shrinkDuration * 0.9f,
                    Ease.InBack,
                    startDelay: shrinkDuration * 0.1f
                ));

            await _countdownSequence.ToUniTask(cancellationToken: ct);
        }

        private void ShowCountdownImage()
        {
            _countdownImage.transform.localScale = _initialCountdownScale;
            _countdownImage.transform.localRotation = Quaternion.identity;
        }

        public void ShowConfirmation(int activePlayerCount)
        {
            _countdownImage.gameObject.SetActive(false);
            _raceGameObject.SetActive(false);

            foreach (var slot in _playerSlots)
            {
                slot.AButtonImage.gameObject.SetActive(false);
                slot.OkImage.gameObject.SetActive(false);
                slot.Animator.enabled = false;
                slot.IsActive = false;
            }

            StartButtonsAnimation(activePlayerCount);
        }

        public void OnConfirmation() => HideConfirmation(_cts.Token).Forget();

        private async UniTask HideConfirmation(CancellationToken ct)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.25f), cancellationToken: ct);

            foreach (var slot in _playerSlots)
            {
                if (!slot.IsActive) continue;

                slot.ATween.Stop();
                slot.ScaleTween.Stop();

                slot.ScaleTween = Tween.Scale(slot.AnimatorTransform, Vector3.one, Vector3.zero, _hideDuration, _slotEaseIn)
                    .OnComplete(() => slot.Animator.enabled = false);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(_hideDuration), cancellationToken: ct);

            await UniTask.Delay(TimeSpan.FromSeconds(3), cancellationToken: ct);

            await PlayCountdown();
        }

        private void StartButtonsAnimation(int playerCount)
        {
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                var slot = _playerSlots[i];
                slot.IsActive = i < playerCount;

                slot.Animator.gameObject.SetActive(slot.IsActive);
                slot.AButtonImage.gameObject.SetActive(slot.IsActive);
                slot.OkImage.gameObject.SetActive(false);

                if (slot.ATween.isAlive)
                    slot.ATween.Stop();
                if (slot.ScaleTween.isAlive)
                    slot.ScaleTween.Stop();

                if (!slot.IsActive) continue;

                slot.ScaleTween = Tween.Scale(slot.AnimatorTransform, Vector3.zero, Vector3.one, _defaultScaleDuration,
                    _actionButtonEaseOut);

                slot.ATween = Tween.Scale(
                    target: slot.AButtonImage.rectTransform,
                    Vector3.one * _pulseScale,
                    duration: _pulseDuration,
                    ease: _actionImageEaseInOut,
                    cycles: -1,
                    cycleMode: CycleMode.Yoyo
                );
            }
        }

        public void NotifyPlayerConfirmed(int playerIndex)
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
            public Transform AnimatorTransform;

            public bool IsActive;

            public Tween ATween;
            public Tween ScaleTween;
        }
    }
}