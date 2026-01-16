using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace MortierFu
{
    public class RoundAnnouncementUI : MonoBehaviour
    {
        #region Assets

        [Header("Countdown Sprites")] [SerializeField]
        private List<Sprite> _countdownSprites;

        private ShakeService _shakeService;

        #endregion

        private void Awake()
        {
            _initialCountdownScale = _countdownImage.transform.localScale;

            _playGameObject.SetActive(false);
            _goldenBombshellGameObject.SetActive(false);
            _countdownImage.gameObject.SetActive(false);
        }

        private void Start()
        {
            _shakeService = ServiceManager.Instance.Get<ShakeService>();
        }

        private void OnDisable()
        {
            if (_countdownSequence.isAlive)
                _countdownSequence.Stop();
        }

        public void OnRoundStarted(GameModeBase gm)
        {
            UpdateMatchPointIndicator(gm);
            PlayCountdown(gm).Forget();
        }

        private void UpdateMatchPointIndicator(GameModeBase gm)
        {
            if (gm == null || _goldenBombshellGameObject.activeSelf) return;

            bool isMatchPoint = false;
            for (int i = 0; i < gm.Teams.Count; i++)
            {
                if (gm.Teams[i].Score >= gm.Data.ScoreToWin)
                {
                    isMatchPoint = true;
                    break;
                }
            }

            _goldenBombshellGameObject.SetActive(isMatchPoint);
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_GameplayUI_MatchPoint);
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


        private async UniTask PlayCountdown(GameModeBase gm, int seconds = 3)
        {
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

            await UniTask.Delay(TimeSpan.FromSeconds(_playShowDelay));
            await ShowPlay(gm);
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

        private async UniTask ShowPlay(GameModeBase gm)
        {
            var t = _playGameObject.transform;

            Vector3 targetPos = t.position;
            Vector3 startPos = targetPos + Vector3.up * _playDropOffset;

            t.position = startPos;
            t.localScale = Vector3.one * _playStartingScale;
            _playCanvasGroup.alpha = 0f;

            _playGameObject.SetActive(true);

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

            await Tween.Scale(
                t,
                Vector3.one * _playStartingScale,
                Vector3.one * _playScaleUp,
                0.2f,
                Ease.OutBack
            );

            await Tween.Alpha(
                _playCanvasGroup,
                1f,
                0f,
                _playFadeOutDuration,
                Ease.InQuad
            );

            _playGameObject.SetActive(false);
            gameObject.SetActive(false);
            _shakeService.ShakeControllers(ShakeService.ShakeType.MID);

            // TODO: Désolé c'est horrible
            gm?.EnablePlayerInputs();
        }

        #region References

        [Header("UI References")] [SerializeField]
        private GameObject _goldenBombshellGameObject;

        [SerializeField] private GameObject _playGameObject;
        [SerializeField] private Image _countdownImage;

        [Header("Canvas Groups")] [SerializeField]
        private CanvasGroup _countdownCanvasGroup;

        [SerializeField] private CanvasGroup _playCanvasGroup;

        #endregion

        #region Countdown Animation

        [Header("Countdown Animation Settings")] [SerializeField]
        private float _countdownSlideOffset = 150f;

        [SerializeField] private float _showCountdownDelay = 0.3f;
        [SerializeField] private float _countdownInDuration = 0.35f;
        [SerializeField] private float _countdownOutDuration = 0.3f;
        [SerializeField] private float _countdownStartingScale = 1.3f;

        private const float COUNTDOWN_TOTAL_DURATION = 1f;

        private float CountdownHoldDuration =>
            COUNTDOWN_TOTAL_DURATION - _countdownInDuration - _countdownOutDuration;

        private Sequence _countdownSequence;
        private Vector3 _initialCountdownScale;

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

        /* private async UniTask AnimateCountdownNumber()
 {
     if (_countdownSequence.isAlive)
         _countdownSequence.Stop();

     _countdownImage.transform.localScale = Vector3.zero;

     _countdownSequence = Sequence.Create()
         .Chain(Tween.Scale(_countdownImage.transform, Vector3.zero, Vector3.one, _countdownGrowthDuration,
             Ease.OutBack))
         .Group(Tween.Rotation(_countdownImage.transform, Quaternion.Euler(0f, 0f, 180),
             Quaternion.Euler(0f, 0f, 0f),
             _countdownGrowthDuration * 0.9f, Ease.OutBack, startDelay: _countdownGrowthDuration * 0.1f))
         .ChainDelay(_countdownBumpDuration)
         .Chain(Tween.Scale(_countdownImage.transform, Vector3.zero, _countdownShrinkDuration, Ease.InBack))
         .Group(Tween.Rotation(_countdownImage.transform, Quaternion.Euler(0f, 0f, 180f),
             _countdownShrinkDuration * 0.9f, Ease.InBack, startDelay: _countdownShrinkDuration * 0.1f));

     await _countdownSequence;
 }*/
    }
}