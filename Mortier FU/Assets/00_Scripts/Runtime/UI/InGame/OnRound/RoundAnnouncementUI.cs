using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace MortierFu
{
    public class RoundAnnouncementUI : MonoBehaviour
    {
        #region Assets

        [Header("Countdown Sprites")]
        [SerializeField] private List<Sprite> _countdownSprites;

        private ShakeService _shakeService;

        #endregion

        private GameModeBase _gameMode;
        private CancellationTokenSource _lifetimeCancellation;

        private void Awake()
        {
            _initialCountdownScale = _countdownImage.transform.localScale;

            HidePresentationObjects();
        }

        private void Start()
        {
            _shakeService = ServiceManager.Instance.Get<ShakeService>();
        }

        private void OnEnable()
        {
            _lifetimeCancellation?.Cancel();
            _lifetimeCancellation?.Dispose();
            _lifetimeCancellation = new CancellationTokenSource();

            SubscribeGameMode();
        }

        private void OnDisable()
        {
            UnsubscribeGameMode();

            StopRunningAnimations();

            _lifetimeCancellation?.Cancel();

            HidePresentationObjects();
        }

        private void OnDestroy()
        {
            UnsubscribeGameMode();

            StopRunningAnimations();

            _lifetimeCancellation?.Cancel();
            _lifetimeCancellation?.Dispose();
            _lifetimeCancellation = null;
        }

        private void SubscribeGameMode()
        {
            UnsubscribeGameMode();

            _gameMode = GameService.CurrentGameMode as GameModeBase;

            if (_gameMode == null)
                return;

            _gameMode.OnRoundStartPresentationAsync += PlayRoundStartPresentationAsync;
        }

        private void UnsubscribeGameMode()
        {
            if (_gameMode == null)
                return;

            _gameMode.OnRoundStartPresentationAsync -= PlayRoundStartPresentationAsync;
            _gameMode = null;
        }

        private void StopRunningAnimations()
        {
            if (_countdownSequence.isAlive)
                _countdownSequence.Stop();
        }

        private void HidePresentationObjects()
        {
            if (_playGameObject)
                _playGameObject.SetActive(false);

            if (_goldenBombshellGameObject)
                _goldenBombshellGameObject.SetActive(false);

            if (_countdownImage)
                _countdownImage.gameObject.SetActive(false);
        }

        public async UniTask PlayRoundStartPresentationAsync(CancellationToken cancellationToken)
        {
            var gameMode = _gameMode ?? GameService.CurrentGameMode as GameModeBase;

            if (gameMode == null)
                return;

            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                _lifetimeCancellation.Token
            );

            var ct = linkedCancellation.Token;

            UpdateMatchPointIndicator(gameMode);

            int countdownSeconds = GetCountdownSeconds(gameMode);
            _countdownStepDuration = GetCountdownStepDuration(gameMode, countdownSeconds);

            await PlayCountdown(gameMode, countdownSeconds, ct);
        }

        private int GetCountdownSeconds(GameModeBase gameMode)
        {
            if (gameMode.FlowSettings)
                return Mathf.Max(0, gameMode.FlowSettings.RoundCountdownSeconds);

            return 3;
        }

        private void UpdateMatchPointIndicator(GameModeBase gm)
        {
            if (gm == null || !_goldenBombshellGameObject)
                return;

            if (_goldenBombshellGameObject.activeSelf)
                return;

            bool isMatchPoint = false;

            for (int i = 0; i < gm.Teams.Count; i++)
            {
                if (gm.Teams[i].Score >= gm.ScoreToWin)
                {
                    isMatchPoint = true;
                    break;
                }
            }

            _goldenBombshellGameObject.SetActive(isMatchPoint);

            if (isMatchPoint)
                AudioService.PlayOneShot(AudioService.FMODEvents.SFX_GameplayUI_MatchPoint);
        }

        private async UniTask AnimateCountdown(CancellationToken ct)
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
                ))
                .ChainDelay(CountdownHoldDuration)
                .Chain(Tween.Alpha(
                    _countdownCanvasGroup,
                    1f,
                    0f,
                    _countdownOutDuration,
                    Ease.InQuad
                ));

            await _countdownSequence;

            ct.ThrowIfCancellationRequested();
        }

        private async UniTask PlayCountdown(
            GameModeBase gm,
            int seconds,
            CancellationToken ct
        )
        {
            ct.ThrowIfCancellationRequested();

            foreach (var character in gm.AlivePlayers)
            {
                if (character)
                    character.gameObject.SetActive(false);
            }

            await UniTask.Delay(
                TimeSpan.FromSeconds(_showCountdownDelay),
                cancellationToken: ct
            );

            ShowCountdownImage();

            if (_countdownSprites.Count > 0)
                _countdownImage.sprite = _countdownSprites[0];

            var countdownTask = RunCountdown(seconds, ct);

            foreach (var character in gm.AlivePlayers)
            {
                ct.ThrowIfCancellationRequested();

                if (!character)
                    continue;

                await character.Aspect.PlayVFXSequential(
                    new[] { character },
                    c => c.gameObject.SetActive(true)
                );

                ct.ThrowIfCancellationRequested();
            }

            await countdownTask;

            _countdownImage.gameObject.SetActive(false);

            await UniTask.Delay(
                TimeSpan.FromSeconds(_playShowDelay),
                cancellationToken: ct
            );

            await ShowPlay(ct);
        }

        private async UniTask RunCountdown(int seconds, CancellationToken ct)
        {
            for (int t = seconds; t > 0; t--)
            {
                ct.ThrowIfCancellationRequested();

                SetCountdownVisual(t);

                if (t > 0)
                    AudioService.PlayOneShot(AudioService.FMODEvents.SFX_GameplayUI_CountdownNumber);

                await AnimateCountdown(ct);

                if (t <= 1)
                    AudioService.PlayOneShot(AudioService.FMODEvents.SFX_GameplayUI_CountdownGo);
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
            int index = Mathf.Clamp(
                _countdownSprites.Count - number,
                0,
                _countdownSprites.Count - 1
            );

            if (_countdownSprites.Count > 0)
                _countdownImage.sprite = _countdownSprites[index];

            _shakeService?.ShakeControllers(ShakeService.ShakeType.MID);
        }

        private async UniTask ShowPlay(CancellationToken ct)
        {
            var t = _playGameObject.transform;

            Vector3 targetPos = t.position;
            Vector3 startPos = targetPos + Vector3.up * _playDropOffset;

            t.position = startPos;
            t.localScale = Vector3.one * _playStartingScale;
            _playCanvasGroup.alpha = 0f;

            _playGameObject.SetActive(true);

            _shakeService?.ShakeControllers(ShakeService.ShakeType.MID);

            ct.ThrowIfCancellationRequested();

            await Sequence.Create()
                .Group(Tween.Position(
                    t,
                    startPos,
                    targetPos,
                    _playPopDuration,
                    Ease.OutCubic
                ))
                .Group(Tween.Alpha(
                    _playCanvasGroup,
                    0f,
                    1f,
                    _playPopDuration,
                    Ease.OutQuad
                ));

            ct.ThrowIfCancellationRequested();

            await Tween.Scale(
                t,
                Vector3.one * _playStartingScale,
                Vector3.one * _playScaleUp,
                0.2f,
                Ease.OutBack
            );

            ct.ThrowIfCancellationRequested();

            await Tween.Alpha(
                _playCanvasGroup,
                1f,
                0f,
                _playFadeOutDuration,
                Ease.InQuad
            );

            ct.ThrowIfCancellationRequested();

            _playGameObject.SetActive(false);
            _countdownImage.gameObject.SetActive(false);
        }
        
        private float GetCountdownStepDuration(GameModeBase gameMode, int countdownSeconds)
        {
            if (countdownSeconds <= 0)
                return 0f;

            if (!gameMode.FlowSettings)
                return k_defaultCountdownStepDuration;

            float totalDuration = Mathf.Max(0.1f, gameMode.FlowSettings.RoundCountdownTotalDuration);

            return Mathf.Max(0.1f, totalDuration / countdownSeconds);
        }

        #region References

        [Header("UI References")]
        [SerializeField] private GameObject _goldenBombshellGameObject;

        [SerializeField] private GameObject _playGameObject;
        [SerializeField] private Image _countdownImage;

        [Header("Canvas Groups")]
        [SerializeField] private CanvasGroup _countdownCanvasGroup;

        [SerializeField] private CanvasGroup _playCanvasGroup;

        #endregion

        #region Countdown Animation

        [Header("Countdown Animation Settings")]
        [SerializeField] private float _countdownSlideOffset = 150f;

        [SerializeField] private float _showCountdownDelay = 0.3f;
        [SerializeField] private float _countdownInDuration = 0.35f;
        [SerializeField] private float _countdownOutDuration = 0.3f;
        [SerializeField] private float _countdownStartingScale = 1.3f;

        private const float k_defaultCountdownStepDuration = 1f;

        private float _countdownStepDuration = k_defaultCountdownStepDuration;

        private float CountdownHoldDuration =>
            Mathf.Max(0f, _countdownStepDuration - _countdownInDuration - _countdownOutDuration);

        private Sequence _countdownSequence;
        private Vector3 _initialCountdownScale;

        #endregion

        #region Play Animation

        [Header("Play Animation Settings")]
        [SerializeField] private float _playDropOffset = 250f;

        [SerializeField] private float _playShowDelay = 0.2f;
        [SerializeField] private float _playPopDuration = 0.4f;
        [SerializeField] private float _playStartingScale = 1.3f;
        [SerializeField] private float _playScaleUp = 1.7f;
        [SerializeField] private float _playFadeOutDuration = 0.3f;

        #endregion
    }
}