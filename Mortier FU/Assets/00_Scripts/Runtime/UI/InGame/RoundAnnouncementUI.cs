using System;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;

namespace MortierFu
{
    public class RoundAnnouncementUI : MonoBehaviour
    {
        [SerializeField] private GameObject _goldenBombshellGameObject;
        [SerializeField] private GameObject _bannerGameObject;
        [SerializeField] private GameObject _readyGameObject;

        [SerializeField] private CountdownUI _countdownUI;

        [SerializeField] private float _slideDuration = 0.5f;
        [SerializeField] private float _readyScaleDuration = 0.5f;
        [SerializeField] private Ease _slideEase = Ease.OutBack;
        [SerializeField] private Ease _readEase = Ease.InElastic;

        private Vector3 _bannerStartPos;
        private Vector3 _bannerCenterPos;
        private Vector3 _readyStartScale;
        private Tween _bannerTween;
        private Tween _readyTween;
        private float _holdDuration;

        private void Awake()
        {
            _bannerStartPos = _bannerGameObject.transform.position;

            _bannerCenterPos = _bannerGameObject.transform.parent.position;
            
            _readyStartScale = _readyGameObject.transform.localScale;

            _bannerGameObject.SetActive(false);
            _readyGameObject.SetActive(false);
            _goldenBombshellGameObject.SetActive(false);
            
            _holdDuration = 3 - (_slideDuration + _readyScaleDuration);
        }

        public void OnRoundStarted(GameModeBase gm)
        {
            UpdateMatchPointIndicator(gm);

            AnimateBanner().Forget();

            _countdownUI.gameObject.SetActive(true);
            _countdownUI.PlayCountdown().Forget();
        }

        private async UniTaskVoid AnimateBanner()
        {
            if (_bannerTween.isAlive)
                _bannerTween.Stop();
            
            if( _readyTween.isAlive)
                _readyTween.Stop();

            _bannerGameObject.SetActive(true);
            _bannerGameObject.transform.position = _bannerStartPos;

            _bannerTween = Tween.Position(
                _bannerGameObject.transform,
                _bannerStartPos,
                _bannerCenterPos,
                _slideDuration,
                _slideEase
            );

            await _bannerTween;

            _readyGameObject.SetActive(true);
            _readyGameObject.transform.localScale = Vector3.zero;
            
            _readyTween = Tween.Scale(
                _readyGameObject.transform,
                Vector3.zero,
                _readyStartScale,
                _readyScaleDuration,
                Ease.OutBounce
            );
            
            await _readyTween;
            
            await UniTask.Delay(TimeSpan.FromSeconds(_holdDuration));

            _readyTween = Tween.Scale(
                _readyGameObject.transform,
                _readyGameObject.transform.localScale,
                Vector3.zero,
                _readyScaleDuration,
                _readEase
            );
            
            await _readyTween;
            
            _bannerTween = Tween.Position(
                _bannerGameObject.transform,
                _bannerCenterPos,
                _bannerStartPos,
                _slideDuration,
                Ease.InQuad
            );

            await _bannerTween;

            _bannerGameObject.SetActive(false);
            _readyGameObject.SetActive(false);
        }

        private void UpdateMatchPointIndicator(GameModeBase gm)
        {
            if (gm == null || _goldenBombshellGameObject.activeSelf) return;

            bool isMatchPoint = false;

            for (int i = 0; i < gm.Teams.Count; i++)
            {
                if (gm.Teams[i].Score < gm.Data.ScoreToWin) continue;
                isMatchPoint = true;
                break;
            }

            _goldenBombshellGameObject.SetActive(isMatchPoint);
        }
    }
}