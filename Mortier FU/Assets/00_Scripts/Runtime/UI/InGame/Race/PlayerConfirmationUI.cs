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

        [SerializeField] private CanvasGroup _countdownCanvasGroup;

        [SerializeField] private CanvasGroup _raceCanvasGroup;

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

        [Header("Countdown Animation Settings")] [SerializeField]
        private float _countdownSlideOffset = 150f;

        [SerializeField] private float _showCountdownDelay = 0.3f;
        [SerializeField] private float _countdownInDuration = 0.35f;
        [SerializeField] private float _countdownOutDuration = 0.3f;
        [SerializeField] private float _countdownStartingScale = 1.3f;

        private const float COUNTDOWN_TOTAL_DURATION = 1f;

        private float CountdownHoldDuration =>
            COUNTDOWN_TOTAL_DURATION - _countdownInDuration - _countdownOutDuration;

        #endregion

        #region Ease Settings

        [Header("Ease Settings")] [SerializeField]
        private Ease _actionButtonEaseOut = Ease.OutBack;

        [SerializeField] private Ease _actionImageEaseInOut = Ease.InOutQuad;
        [SerializeField] private Ease _readyEaseIn = Ease.InBack;
        [SerializeField] private Ease _slotEaseIn = Ease.InQuint;

        [Header("Ready Animation Settings")] [SerializeField]
        private float _readyDropOffset = 250f;

        [SerializeField] private float _readyShowDelay = 0.2f;
        [SerializeField] private float _readyPopDuration = 0.4f;
        [SerializeField] private float _readyStartingScale = 1.3f;
        [SerializeField] private float _readyScaleUp = 1.7f;
        [SerializeField] private float _readyFadeOutDuration = 0.3f;

        #endregion

        #region Runtime State

        private Vector3 _initialCountdownScale;
        private ShakeService _shakeService;

        private Sequence _countdownSequence;

        private CancellationTokenSource _cts;

        #endregion

        #endregion

        private void Awake()
        {
            _initialCountdownScale = _countdownImage.transform.localScale;

            _raceGameObject.SetActive(false);
            _countdownImage.gameObject.SetActive(false);
        }

        private void Start()
        {
            _shakeService = ServiceManager.Instance.Get<ShakeService>();
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

        private async UniTask AnimateCountdown()
        {
            if (_countdownSequence.isAlive)
                _countdownSequence.Stop();

            var t = _countdownImage.transform;

            Vector3 centerPos = t.position;
            Vector3 startPos = centerPos + Vector3.up * _countdownSlideOffset;

            t.position = startPos;
            t.localScale = Vector3.one * _countdownStartingScale;
            _countdownCanvasGroup.alpha = 0f;

            _countdownSequence = Sequence.Create()
                .Group(Tween.Position(
                    t,
                    startPos,
                    centerPos,
                    _countdownInDuration,
                    Ease.OutCubic
                ))
                .Group(Tween.Alpha(
                    _countdownCanvasGroup,
                    0f,
                    1f,
                    _countdownInDuration,
                    Ease.OutQuad
                )).ChainDelay(CountdownHoldDuration).Chain(Tween.Alpha(
                    _countdownCanvasGroup,
                    1f,
                    0f,
                    _countdownOutDuration,
                    Ease.InQuad
                ));

            await _countdownSequence;
        }

        private async UniTask PlayCountdown(int seconds = 3)
        {
            var gm = GameService.CurrentGameMode as GameModeBase;

            foreach (var character in gm.AlivePlayers)
            {
                character.gameObject.SetActive(false);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(_showCountdownDelay));

            ShowCountdownImage();
            _countdownImage.sprite = _countdownSprites[0];

            var countdownTask = RunCountdown(seconds);

            foreach (var character in gm.AlivePlayers)
            {
                await character.Aspect.PlayVFXSequential(new[] { character },
                    c => c.gameObject.SetActive(true));
            }

            await countdownTask;

            _countdownImage.gameObject.SetActive(false);

            await UniTask.Delay(TimeSpan.FromSeconds(_readyShowDelay));
            await ShowReady(gm);
        }

        private async UniTask ShowReady(GameModeBase gm)
        {
            var t = _raceGameObject.transform;

            Vector3 targetPos = t.position;
            Vector3 startPos = targetPos + Vector3.up * _readyDropOffset;

            t.position = startPos;
            t.localScale = Vector3.one * _readyStartingScale;
            _raceCanvasGroup.alpha = 0f;

            _raceGameObject.SetActive(true);

            _shakeService.ShakeControllers(ShakeService.ShakeType.MID);

            await Sequence.Create()
                .Group(Tween.Position(
                    t,
                    startPos,
                    targetPos,
                    _readyPopDuration,
                    Ease.OutCubic
                ))
                .Group(Tween.Alpha(
                    _raceCanvasGroup,
                    0f,
                    1f,
                    _readyPopDuration,
                    Ease.OutQuad
                ));

            await Tween.Scale(
                t,
                Vector3.one * _readyStartingScale,
                Vector3.one * _readyScaleUp,
                0.2f,
                Ease.OutBack
            );

            await Tween.Alpha(
                _raceCanvasGroup,
                1f,
                0f,
                _readyFadeOutDuration,
                Ease.InQuad
            );

            _raceGameObject.SetActive(false);
            gameObject.SetActive(false);

            // TODO: Désolé c'est horrible
            gm?.EnablePlayerInputs();
        }


        private async UniTask RunCountdown(int seconds)
        {
            for (int t = seconds; t > 0; t--)
            {
                SetCountdownVisual(t);
                // TODO: Add sound effect here or maybe in AnimateCountdown
                await AnimateCountdown();
            }
        }

        private void ShowCountdownImage()
        {
            _countdownImage.transform.localScale = _initialCountdownScale;
            _countdownImage.transform.localRotation = Quaternion.identity;
            _countdownImage.gameObject.SetActive(true);
        }

        private void SetCountdownVisual(int number)
        {
            int index = Mathf.Clamp(_countdownSprites.Count - number, 0, _countdownSprites.Count - 1);
            _countdownImage.sprite = _countdownSprites[index];
            _shakeService.ShakeControllers(ShakeService.ShakeType.MID);
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

                slot.ScaleTween = Tween.Scale(slot.AnimatorTransform, Vector3.one, Vector3.zero, _hideDuration,
                        _slotEaseIn)
                    .OnComplete(() => slot.Animator.enabled = false);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(_hideDuration), cancellationToken: ct);

            await UniTask.Delay(TimeSpan.FromSeconds(4), cancellationToken: ct);

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