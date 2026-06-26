using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using System;
using System.Threading;
using Vector3 = UnityEngine.Vector3;

namespace MortierFu
{
    public class RoundAnnouncementUI : MonoBehaviour
    {
        private ShakeService _shakeService;

        private GameModeBase _gameMode;
        private CancellationTokenSource _lifetimeCancellation;

        private void Awake()
        {
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

            _lifetimeCancellation?.Cancel();

            HidePresentationObjects();
        }

        private void OnDestroy()
        {
            UnsubscribeGameMode();

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

        private void HidePresentationObjects()
        {
            if (_playGameObject)
                _playGameObject.SetActive(false);

            if (_goldenBombshellGameObject)
                _goldenBombshellGameObject.SetActive(false);
        }

        private async UniTask PlayRoundStartPresentationAsync(CancellationToken cancellationToken)
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

            await PlayCountdown(gameMode, ct);
        }

        private void UpdateMatchPointIndicator(GameModeBase gm)
        {
            if (gm == null || !_goldenBombshellGameObject)
                return;

            if (_goldenBombshellGameObject.activeSelf)
                return;

            var isMatchPoint = false;

            for (var i = 0; i < gm.Teams.Count; i++)
            {
                if (gm.Teams[i].Score < gm.ScoreToWin) continue;
                
                isMatchPoint = true;
                break;
            }

            _goldenBombshellGameObject.SetActive(isMatchPoint);

            if (isMatchPoint)
                AudioService.PlayOneShot(AudioService.FMODEvents.SFX_GameplayUI_MatchPoint);
        }

        private async UniTask PlayCountdown(
            GameModeBase gm,
            CancellationToken ct
        )
        {
            ct.ThrowIfCancellationRequested();

            foreach (var character in gm.AlivePlayers)
            {
                if (character)
                    character.gameObject.SetActive(false);
            }

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

            await UniTask.Delay(
                TimeSpan.FromSeconds(_playShowDelay),
                cancellationToken: ct
            );

            await ShowPlay(ct);
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
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_GameplayUI_CountdownGo);

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
        }

        #region References

        [Header("UI References")] [SerializeField]
        private GameObject _goldenBombshellGameObject;

        [SerializeField] private GameObject _playGameObject;

        [Header("Canvas Groups")] [SerializeField]
        private CanvasGroup _countdownCanvasGroup;

        [SerializeField] private CanvasGroup _playCanvasGroup;

        #endregion

        #region Play Animation

        [Header("Play Animation Settings")] [SerializeField]
        private float _playDropOffset = 250f;

        [SerializeField] private float _playShowDelay = 0.2f;
        [SerializeField] private float _playPopDuration = 0.4f;
        [SerializeField] private float _playStartingScale = 1.3f;
        [SerializeField] private float _playScaleUp = 1.7f;
        [SerializeField] private float _playFadeOutDuration = 0.3f;

        #endregion
    }
}