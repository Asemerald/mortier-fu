using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace MortierFu
{
    public class RoundAnnouncementUI : MonoBehaviour
    {
        [SerializeField] private GameObject _goldenBombshellGameObject;
        [SerializeField] private GameObject _bannerGameObject;
        [SerializeField] private GameObject _readyGameObject;
        [SerializeField] private GameObject _playGameObject;
        [SerializeField] private Image _countdownImage;
        [SerializeField] private List<Sprite> _countdownSprites;

        [SerializeField] float _slideDuration = 0.5f;
        [SerializeField] Ease _slideEase = Ease.OutBack;

        [SerializeField] float _readyScaleDuration = 0.5f;
        [SerializeField] Ease _readyEaseIn = Ease.InElastic;
        [SerializeField] Ease _readyEaseOut = Ease.OutBounce;

        [SerializeField] float _holdDuration = 1f;

        [SerializeField] float _countdownGrowthDuration = 0.3f;
        [SerializeField] float _countdownShrinkDuration = 0.3f;
        [SerializeField] float _countdownBumpDuration = 0.4f;

        [SerializeField] float _racePopDuration = 0.5f;
        [SerializeField] float _playDisableDelay = 1f;
        
        private Vector3 _bannerCenterPos;
        private Vector3 _bannerStartPos;
        private Tween _bannerTween;

        private Sequence _countdownSequence;
        private Vector3 _initialCountdownScale;

        private Vector3 _initialPlayScale;
        private Vector3 _readyStartScale;
        private Tween _readyTween;

        private void Awake()
        {
            _bannerStartPos = _bannerGameObject.transform.position;
            _bannerCenterPos = _bannerGameObject.transform.parent.position;

            _readyStartScale = _readyGameObject.transform.localScale;
            _initialCountdownScale = _countdownImage.transform.localScale;
            _initialPlayScale = _playGameObject.transform.localScale;

            _playGameObject.SetActive(false);
            _bannerGameObject.SetActive(false);
            _readyGameObject.SetActive(false);
            _goldenBombshellGameObject.SetActive(false);
            _countdownImage.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (_bannerTween.isAlive)
                _bannerTween.Stop();

            if (_readyTween.isAlive)
                _readyTween.Stop();

            if (_countdownSequence.isAlive)
                _countdownSequence.Stop();
        }

        public void OnRoundStarted(GameModeBase gm)
        {
            UpdateMatchPointIndicator(gm);
            AnimateBanner().Forget();
        }

        private async UniTaskVoid AnimateBanner()
        {
            if (_bannerTween.isAlive)
                _bannerTween.Stop();
            if (_readyTween.isAlive)
                _readyTween.Stop();

            _bannerGameObject.transform.position = _bannerStartPos;
            _bannerGameObject.SetActive(true);
            
            _readyGameObject.transform.localScale = Vector3.zero;
            _readyGameObject.SetActive(true);

            _bannerTween = Tween.Position(
                _bannerGameObject.transform,
                _bannerGameObject.transform.position,
                _bannerCenterPos,
                _slideDuration,
                _slideEase
            );
            await _bannerTween;

            _readyTween = Tween.Scale(
                _readyGameObject.transform,
                Vector3.zero,
                _readyStartScale,
                _readyScaleDuration,
                _readyEaseOut
            );
            await _readyTween;

            await UniTask.Delay(TimeSpan.FromSeconds(_holdDuration));

            _readyTween = Tween.Scale(
                _readyGameObject.transform,
                _readyGameObject.transform.localScale,
                Vector3.zero,
                _readyScaleDuration,
                _readyEaseIn
            );
            await _readyTween;
            
            _bannerTween = Tween.Position(
                _bannerGameObject.transform,
                _bannerGameObject.transform.position,
                _bannerStartPos,
                _slideDuration,
                Ease.InQuad
            );
            await _bannerTween;

            _bannerGameObject.SetActive(false);
            _readyGameObject.SetActive(false);

            await UniTask.Delay(TimeSpan.FromSeconds(1f));

            PlayCountdown().Forget();
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
        }

        private async UniTask PlayCountdown(int seconds = 3)
        {
            ShowCountdownImage();
            _countdownImage.sprite = _countdownSprites[0];

            for (int t = seconds; t > 0; t--)
            {
                SetCountdownVisual(t);
                await AnimateCountdownNumber();
            }

            _countdownImage.gameObject.SetActive(false);

            await UniTask.Delay(TimeSpan.FromSeconds(1));
            await ShowPlay();
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
        }

        private async UniTask AnimateCountdownNumber()
        {
            if (_countdownSequence.isAlive)
                _countdownSequence.Stop();

            _countdownImage.transform.localScale = Vector3.zero;

            _countdownSequence = Sequence.Create()
                .Chain(Tween.Scale(_countdownImage.transform, Vector3.zero, Vector3.one, _countdownGrowthDuration, Ease.OutBack))
                .Group(Tween.Rotation(_countdownImage.transform, Quaternion.Euler(0f, 0f, 180),
                    Quaternion.Euler(0f, 0f, 0f),
                    _countdownGrowthDuration * 0.9f, Ease.OutBack, startDelay: _countdownGrowthDuration * 0.1f))
                .ChainDelay(_countdownBumpDuration)
                .Chain(Tween.Scale(_countdownImage.transform, Vector3.zero, _countdownShrinkDuration, Ease.InBack))
                .Group(Tween.Rotation(_countdownImage.transform, Quaternion.Euler(0f, 0f, 180f),
                    _countdownShrinkDuration * 0.9f, Ease.InBack, startDelay: _countdownShrinkDuration * 0.1f));

            await _countdownSequence;
        }

        private async UniTask ShowPlay()
        {
            _playGameObject.SetActive(true);
            _playGameObject.transform.localScale = Vector3.zero;

            await Tween.Scale(
                _playGameObject.transform,
                Vector3.zero,
                _initialPlayScale,
                _racePopDuration,
                Ease.InBack
            );

            await UniTask.Delay(TimeSpan.FromSeconds(_playDisableDelay));

            _playGameObject.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}