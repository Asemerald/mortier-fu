using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using System;
using System.Threading;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace MortierFu
{
    public class RoundAnnouncementUI : MonoBehaviour
    {
        #region References

        [Header("UI References")]
        [SerializeField] private GameObject _matchpoint;
        [SerializeField] private GameObject _playGameObject;

        [Header("Canvas Groups")]
        [SerializeField] private CanvasGroup _countdownCanvasGroup;
        [SerializeField] private CanvasGroup _matchpointCanvasGroup;
        [SerializeField] private CanvasGroup _playCanvasGroup;

        #endregion

        #region Match Point Exit Animation

        [Header("Match Point Banner Exit Settings")]
        [SerializeField] private float _matchpointExitDelay = 0.8f;
        [SerializeField] private float _matchpointExitDuration = 0.35f;
        [SerializeField] private Vector2 _matchpointExitOffset = new Vector2(1920f, 0f);
        [SerializeField] private Ease _matchpointExitEase = Ease.InCubic;
        [SerializeField] private bool _matchpointFadeOut = true;

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

        private ShakeService _shakeService;

        private GameModeBase _gameMode;
        private CancellationTokenSource _lifetimeCancellation;

        private Vector3 _matchpointInitialLocalPos;
        private bool _matchpointPosCached;

        private void Awake()
        {
            CacheMatchpointPosition();
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

        private void CacheMatchpointPosition()
        {
            if (_matchpointPosCached || _matchpoint == null)
                return;

            _matchpointInitialLocalPos = _matchpoint.transform.localPosition;
            _matchpointPosCached = true;
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

            if (_matchpoint)
            {
                if (_matchpointPosCached)
                    _matchpoint.transform.localPosition = _matchpointInitialLocalPos;

                if (_matchpointCanvasGroup != null)
                    _matchpointCanvasGroup.alpha = 1f;

                _matchpoint.SetActive(false);
            }
        }

        private async UniTask PlayRoundStartPresentationAsync(CancellationToken cancellationToken)
        {
            var gameMode = _gameMode ?? GameService.CurrentGameMode as GameModeBase;

            if (gameMode == null)
                return;

            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetimeCancellation.Token);

            var ct = linkedCancellation.Token;

            UpdateMatchPointIndicator(gameMode, ct);

            await PlayCountdown(gameMode, ct);
        }

        private void UpdateMatchPointIndicator(GameModeBase gm, CancellationToken ct)
        {
            if (gm == null || !_matchpoint)
                return;

            var isMatchPoint = false;

            for (var i = 0; i < gm.Teams.Count; i++)
            {
                if (gm.Teams[i].Score < gm.ScoreToWin) continue;

                isMatchPoint = true;
                break;
            }

            if (!isMatchPoint)
            {
                _matchpoint.SetActive(false);
                return;
            }

            CacheMatchpointPosition();

            _matchpoint.transform.localPosition = _matchpointInitialLocalPos;

            if (_matchpointCanvasGroup != null)
                _matchpointCanvasGroup.alpha = 1f;

            _matchpoint.SetActive(true);
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_GameplayUI_MatchPoint);

            AnimateMatchpointExitAsync(ct).Forget();
        }

        private async UniTaskVoid AnimateMatchpointExitAsync(CancellationToken ct)
        {
            try
            {
                if (_matchpointExitDelay > 0f)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_matchpointExitDelay),
                        cancellationToken: ct
                    );
                }

                if (_matchpoint == null || !_matchpoint.activeSelf)
                    return;

                if (_matchpointExitDuration > 0f)
                {
                    Vector3 startLocalPos = _matchpointInitialLocalPos;
                    Vector3 targetLocalPos = _matchpointInitialLocalPos + (Vector3)_matchpointExitOffset;

                    Sequence seq = Sequence.Create()
                        .Group(Tween.LocalPosition(
                            _matchpoint.transform,
                            startLocalPos,
                            targetLocalPos,
                            _matchpointExitDuration,
                            _matchpointExitEase
                        ));

                    if (_matchpointFadeOut && _matchpointCanvasGroup != null)
                    {
                        seq.Group(Tween.Alpha(
                            _matchpointCanvasGroup,
                            1f,
                            0f,
                            _matchpointExitDuration,
                            _matchpointExitEase
                        ));
                    }

                    await seq.ToUniTask(cancellationToken: ct);
                }

                if (_matchpoint != null)
                {
                    _matchpoint.SetActive(false);
                    _matchpoint.transform.localPosition = _matchpointInitialLocalPos;
                }

                if (_matchpointCanvasGroup != null)
                    _matchpointCanvasGroup.alpha = 1f;
            }
            catch (OperationCanceledException)
            {
                // Annulation normale lors du changement d'état ou de la désactivation
            }
        }

        private async UniTask PlayCountdown(GameModeBase gm, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            foreach (var character in gm.AlivePlayers)
            {
                if (character)
                    character.gameObject.SetActive(false);
            }

            await CircleTransition.Instance.OpenAsync(_gameMode.FlowSettings.RaceTransitionDuration);
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
            //_playCanvasGroup.alpha = 0f;

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
                    1f,
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
                1f,
                _playFadeOutDuration,
                Ease.InQuad
            );

            ct.ThrowIfCancellationRequested();

            HidePresentationObjects();
        }
    }
}