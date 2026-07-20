using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

namespace MortierFu
{
    public class PlayerConfirmationUI : MonoBehaviour
    {
        [Header("Player Slots (Blue, Red, Green, Yellow)")] [SerializeField]
        private List<PlayerSlot> _playerSlots;

        [Header("UI References")]
        [SerializeField] private GameObject _raceGameObject;

        [SerializeField] private CanvasGroup _countdownCanvasGroup;

        [SerializeField] private CanvasGroup _raceCanvasGroup;

        [Header("General Animation Settings")] [SerializeField]
        private float _pulseScale = 1.15f;

        [SerializeField] private float _pulseDuration = 0.45f;
        [SerializeField] private float _hideDuration = 0.6f;
        [SerializeField] private float _defaultScaleDuration = 0.5f;

        [Header("Ease Settings")] [SerializeField]
        private Ease _actionButtonEaseOut = Ease.OutBack;

        [SerializeField] private Ease _actionImageEaseInOut = Ease.InOutQuad;
        [SerializeField] private Ease _slotEaseIn = Ease.InQuint;

        [Header("Ready Animation Settings")] [SerializeField]
        private float _readyDropOffset;

        [SerializeField] private float _readyShowDelay = 0.1f;
        [SerializeField] private float _readyPopDuration = 0.5f;
        [SerializeField] private float _readyStartingScale = 0.8f;
        [SerializeField] private float _readyScaleUp = 1.4f;
        [SerializeField] private float _readyFadeOutDuration = 0.3f;
        
        [Header("Confirmed Spam Feedback")]
        [SerializeField] private float _confirmedSpamScaleDown = 0.82f;
        [SerializeField] private float _confirmedSpamDownDuration = 0.05f;
        [SerializeField] private float _confirmedSpamUpDuration = 0.12f;
        [SerializeField] private Ease _confirmedSpamDownEase = Ease.InQuad;
        [SerializeField] private Ease _confirmedSpamUpEase = Ease.OutBack;

        private ShakeService _shakeService;
        private GameModeBase _gm;

        private UniTaskCompletionSource<bool> _confirmationHideCompletion;
        private bool _isHidingConfirmation;
        
        private CancellationTokenSource _cts;

        private void Awake()
        {
            _raceGameObject.SetActive(false);
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

            SubscribeGameMode();
        }

        private void OnDisable()
        {
            UnsubscribeGameMode();

            CleanupTweens();

            _isHidingConfirmation = false;
            
            _confirmationHideCompletion?.TrySetResult(true);
            _confirmationHideCompletion = null;

            _cts?.Cancel();
        }

        private void OnDestroy()
        {
            CleanupTweens();
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private void SubscribeGameMode()
        {
            UnsubscribeGameMode();

            _gm = GameService.CurrentGameMode as GameModeBase;

            if (_gm == null)
                return;

            _gm.OnAugmentRaceStartPresentationAsync += PlayAugmentRaceStartPresentationAsync;
        }

        private void UnsubscribeGameMode()
        {
            if (_gm == null)
                return;

            _gm.OnAugmentRaceStartPresentationAsync -= PlayAugmentRaceStartPresentationAsync;
            _gm = null;
        }

        private void CleanupTweens()
        {
            if (_playerSlots == null)
                return;

            for (int i = 0; i < _playerSlots.Count; i++)
            {
                StopSlotTweens(_playerSlots[i]);
            }
        }
        
        private static void StopSlotTweens(PlayerSlot slot)
        {
            if (slot == null)
                return;

            slot.ATween.Stop();
            slot.ScaleTween.Stop();
            slot.ConfirmedFeedbackTween.Stop();
        }

        private async UniTask PlayCountdown(GameModeBase gm, CancellationToken ct, int seconds = 0)
        {
            foreach (var character in gm.AlivePlayers)
            {
                character.gameObject.SetActive(false);
            }

            foreach (var character in gm.AlivePlayers)
            {
                await character.Aspect.PlayVFXSequential(new[] { character },
                    c => c.gameObject.SetActive(true));
            }

            await UniTask.Delay(TimeSpan.FromSeconds(_readyShowDelay), cancellationToken: ct);
            await ShowReady(gm, ct);
        }

        private async UniTask ShowReady(GameModeBase gm, CancellationToken ct)
        {
            var t = _raceGameObject.transform;

            Vector3 targetPos = t.position;
            Vector3 startPos = targetPos + Vector3.up * _readyDropOffset;

            t.position = startPos;
            t.localScale = Vector3.one * _readyStartingScale;
            _raceCanvasGroup.alpha = 0f;

            _raceGameObject.SetActive(true);

            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_GameplayUI_CountdownGo);
            _shakeService.ShakeControllers(ShakeService.ShakeType.MID);

            ct.ThrowIfCancellationRequested();

            await Sequence.Create().Group(Tween.Position(t, startPos, targetPos, _readyPopDuration, Ease.OutCubic))
                .Group(Tween.Alpha(_raceCanvasGroup, 0f, 1f, _readyPopDuration, Ease.OutQuad))
                .Group(Tween.Scale(t, Vector3.one * _readyStartingScale, Vector3.one * _readyScaleUp, 0.2f, Ease.OutBack));

            ct.ThrowIfCancellationRequested();

            await Tween.Alpha(_raceCanvasGroup, 1f, 0f, _readyFadeOutDuration, Ease.InQuad);

            ct.ThrowIfCancellationRequested();

            _raceGameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        public void ShowConfirmation(int activePlayerCount)
        {
            _isHidingConfirmation = false;

            _confirmationHideCompletion?.TrySetResult(true);
            _confirmationHideCompletion = null;

            if (_raceGameObject)
                _raceGameObject.SetActive(false);

            InitializeSlots(activePlayerCount);

            for (int i = 0; i < _playerSlots.Count; i++)
            {
                ResetSlotVisualState(_playerSlots[i]);
            }

            StartButtonsAnimation(activePlayerCount);
        }

        public void OnConfirmation()
        {
            if (_isHidingConfirmation)
                return;

            _isHidingConfirmation = true;

            _confirmationHideCompletion = new UniTaskCompletionSource<bool>();
            HideConfirmation(_cts.Token, _confirmationHideCompletion).Forget();
        }

        private async UniTask HideConfirmation(CancellationToken ct, UniTaskCompletionSource<bool> completion)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.25f), cancellationToken: ct);

                for (int i = 0; i < _playerSlots.Count; i++)
                {
                    PlayerSlot slot = _playerSlots[i];

                    if (!slot.IsActive)
                        continue;

                    StopSlotTweens(slot);

                    if (!slot.AnimatorTransform)
                        continue;

                    slot.ScaleTween = Tween.Scale(slot.AnimatorTransform, Vector3.one, Vector3.zero, _hideDuration, _slotEaseIn)
                        .OnComplete(() =>
                        {
                            if (slot.Animator) slot.Animator.enabled = false;
                        });
                }

                await UniTask.Delay(TimeSpan.FromSeconds(_hideDuration), cancellationToken: ct);
            }
            catch (OperationCanceledException)
            { }
            finally
            {
                _isHidingConfirmation = false;
                completion?.TrySetResult(true);
            }
        }

        private async UniTask WaitForConfirmationHideAsync(CancellationToken ct)
        {
            var completion = _confirmationHideCompletion;

            if (completion == null)
                return;

            await completion.Task;

            ct.ThrowIfCancellationRequested();
        }

        public async UniTask PlayAugmentRaceStartPresentationAsync(CancellationToken ct)
        {
            if (GameService.CurrentGameMode is not GameModeBase gm)
                return;
            
            await WaitForConfirmationHideAsync(ct);

            float delayAfterConfirmation = gm.FlowSettings.AugmentRaceStartDelayAfterConfirmation;

            if (delayAfterConfirmation > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(delayAfterConfirmation), cancellationToken: ct);

            ct.ThrowIfCancellationRequested();

            var presentationTask = PlayCountdown(gm, ct);

            await presentationTask;

            ct.ThrowIfCancellationRequested();
        }

        private void StartButtonsAnimation(int playerCount)
        {
            int activeCount = Mathf.Clamp(playerCount, 0, _playerSlots.Count);

            for (int i = 0; i < _playerSlots.Count; i++)
            {
                PlayerSlot slot = _playerSlots[i];

                slot.IsActive = i < activeCount;

                if (slot.Animator && slot.Animator.gameObject)
                    slot.Animator.gameObject.SetActive(slot.IsActive);

                if (!slot.IsActive)
                    continue;

                Image buttonImage = slot.ConfirmationButtonImageTarget ? slot.ConfirmationButtonImageTarget : slot.GamePadInputImage;

                if (!buttonImage)
                {
                    Logs.LogWarning($"[PlayerConfirmationUI] Missing confirmation button image for slot {i}.");
                    continue;
                }

                slot.ConfirmationButtonImageTarget = buttonImage;

                buttonImage.gameObject.SetActive(true);
                buttonImage.rectTransform.localScale = Vector3.one;

                if (slot.OkImage)
                    slot.OkImage.gameObject.SetActive(false);

                if (slot.AnimatorTransform)
                {
                    slot.AnimatorTransform.localScale = Vector3.zero;

                    slot.ScaleTween = Tween.Scale(slot.AnimatorTransform, Vector3.zero, Vector3.one, _defaultScaleDuration, _actionButtonEaseOut); 
                }

                slot.ATween = Tween.Scale(target: buttonImage.rectTransform, Vector3.one * _pulseScale, duration: _pulseDuration, ease: _actionImageEaseInOut, cycles: -1, cycleMode: CycleMode.Yoyo);
            }
        }

        public void NotifyPlayerConfirmed(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= _playerSlots.Count)
                return;

            PlayerSlot slot = _playerSlots[playerIndex];

            if (!slot.IsActive)
                return;

            slot.HasConfirmed = true;

            slot.ATween.Stop();

            if (slot.ConfirmationButtonImageTarget)
            {
                slot.ConfirmationButtonImageTarget.rectTransform.localScale = Vector3.one;
                slot.ConfirmationButtonImageTarget.gameObject.SetActive(false);
            }

            if (slot.OkImage)
                slot.OkImage.rectTransform.localScale = Vector3.one;

            if (slot.Animator)
            {
                slot.Animator.enabled = true;
                slot.Animator.Play(0, 0, 0f);
            }

            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Ready);
        }

        public void NotifyConfirmedPlayerPressedAgain(int playerIndex)
        {
            if (_isHidingConfirmation)
                return;

            if (playerIndex < 0 || playerIndex >= _playerSlots.Count)
                return;

            PlayerSlot slot = _playerSlots[playerIndex];

            if (!slot.IsActive || !slot.HasConfirmed)
                return;

            Transform feedbackTarget = ResolveConfirmedFeedbackTarget(slot);

            if (!feedbackTarget)
                return;

            slot.ConfirmedFeedbackTween.Stop();

            feedbackTarget.localScale = Vector3.one;

            slot.ConfirmedFeedbackTween = Tween.Scale(feedbackTarget, Vector3.one * _confirmedSpamScaleDown, _confirmedSpamDownDuration, _confirmedSpamDownEase)
                .OnComplete(() =>
            {
                if (!feedbackTarget)
                    return;

                slot.ConfirmedFeedbackTween = Tween.Scale(feedbackTarget, Vector3.one, _confirmedSpamUpDuration, _confirmedSpamUpEase);
            });
        }
        
        private void InitializeSlots(int activePlayerCount)
        {
            if (_gm == null)
            {
                Logs.LogError("[PlayerConfirmationUI] No GameModeBase found.");
                return;
            }

            int activeCount = Mathf.Clamp(activePlayerCount, 0, _playerSlots.Count);

            for (int i = 0; i < _playerSlots.Count; i++)
            {
                PlayerSlot slot = _playerSlots[i];

                slot.ConfirmationButtonImageTarget = slot.GamePadInputImage;

                if (slot.KeyBoardInputImage)
                    slot.KeyBoardInputImage.gameObject.SetActive(false);

                if (slot.GamePadInputImage)
                    slot.GamePadInputImage.gameObject.SetActive(false);
            }

            for (int i = 0; i < activeCount; i++)
            {
                if (_gm.Teams == null || i >= _gm.Teams.Count)
                    continue;

                PlayerTeam team = _gm.Teams[i];

                if (team?.Members == null || team.Members.Count == 0 || !team.Members[0])
                    continue;

                bool isKeyboardUser = team.Members[0].IsKeyboardAndMouseControlScheme();

                _playerSlots[i].ConfirmationButtonImageTarget = isKeyboardUser ? _playerSlots[i].KeyBoardInputImage : _playerSlots[i].GamePadInputImage;
            }
        }
        
        private void ResetSlotVisualState(PlayerSlot slot)
        {
            if (slot == null)
                return;

            StopSlotTweens(slot);

            slot.IsActive = false;
            slot.HasConfirmed = false;

            if (slot.Animator)
                slot.Animator.enabled = false;

            if (slot.AnimatorTransform)
                slot.AnimatorTransform.localScale = Vector3.zero;

            if (slot.GamePadInputImage)
            {
                slot.GamePadInputImage.gameObject.SetActive(false);
                slot.GamePadInputImage.rectTransform.localScale = Vector3.one;
            }

            if (slot.KeyBoardInputImage)
            {
                slot.KeyBoardInputImage.gameObject.SetActive(false);
                slot.KeyBoardInputImage.rectTransform.localScale = Vector3.one;
            }

            if (slot.OkImage)
            {
                slot.OkImage.gameObject.SetActive(false);
                slot.OkImage.rectTransform.localScale = Vector3.one;
            }

            if (!slot.ConfirmationButtonImageTarget && slot.GamePadInputImage)
                slot.ConfirmationButtonImageTarget = slot.GamePadInputImage;
        }

        private static Transform ResolveConfirmedFeedbackTarget(PlayerSlot slot)
        {
            if (slot.OkImage && slot.OkImage.gameObject.activeInHierarchy)
                return slot.OkImage.rectTransform;

            return slot.AnimatorTransform;
        }
        
        [Serializable]
        public class PlayerSlot
        {
            [HideInInspector] public Image ConfirmationButtonImageTarget;
            
            public Image GamePadInputImage;
            public Image KeyBoardInputImage;

            public Image OkImage;

            public Animator Animator;
            public Transform AnimatorTransform;

            public bool IsActive;
            public bool HasConfirmed;

            public Tween ATween;
            public Tween ScaleTween;
            public Tween ConfirmedFeedbackTween;
        }
    }
}