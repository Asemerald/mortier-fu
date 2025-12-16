using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;
using System;

namespace MortierFu
{
    public class CountdownUI : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private Image _countdownImage;

        [SerializeField] private List<Sprite> _countdownSprites;

        [SerializeField] private GameObject _lastGameObjectToShow;
        [SerializeField] private GameObject _firstGameObjectToShow;

        [Header("Parameters")] [SerializeField]
        private float _racePopDuration = 1f;

        [SerializeField] private float _racePopScale = 1.2f;

        private Vector3 _initialCountdownScale;

        private Sequence _countdownSequence;

        private void Awake()
        {
            _initialCountdownScale = _countdownImage.transform.localScale;

            if (_lastGameObjectToShow != null)
                _lastGameObjectToShow.SetActive(false);

            if (_firstGameObjectToShow != null)
                _firstGameObjectToShow.SetActive(false);

            _countdownImage.gameObject.SetActive(false);
        }

        public async UniTask PlayCountdown(int seconds = 3)
        {
            ResetUI();
            ShowCountdownImage();

            if (_firstGameObjectToShow != null)
                _firstGameObjectToShow.SetActive(true);

            for (int t = seconds; t > 0; t--)
            {
                SetCountdownVisual(t);
                AnimateCountdownNumber().Forget();
                await UniTask.Delay(TimeSpan.FromSeconds(1));
            }

            _countdownImage.gameObject.SetActive(false);

            if (_lastGameObjectToShow != null)
            {
                await ShowRaceObject();
            }
        }

        private void ShowCountdownImage()
        {
            _countdownImage.gameObject.SetActive(true);
            _countdownImage.transform.localScale = _initialCountdownScale;
            _countdownImage.transform.localRotation = Quaternion.identity;
        }

        private void SetCountdownVisual(int number)
        {
            int index = Mathf.Clamp(_countdownSprites.Count - number, 0, _countdownSprites.Count - 1);
            _countdownImage.sprite = _countdownSprites[index];
        }

        private async UniTask AnimateCountdownNumber()
        {
            var target = _countdownImage.transform;
            Vector3 targetScale = _initialCountdownScale;
            
            if (_countdownSequence.isAlive)
                _countdownSequence.Stop();

            target.localScale = Vector3.zero;

            const float growthDuration = 0.3f;
            const float shrinkDuration = 0.3f;
            float bumpDuration = 0.4f;

            var tcs = new UniTaskCompletionSource();

            _countdownSequence = Sequence.Create(1, Sequence.SequenceCycleMode.Restart)
                .Chain(Tween.Scale(target, Vector3.zero, targetScale, growthDuration, Ease.OutBack))
                .Group(Tween.Rotation(target, Quaternion.Euler(0f, 0f, 180), Quaternion.Euler(0f, 0f, 0f),
                    growthDuration * 0.9f, Ease.OutBack, startDelay: growthDuration * 0.1f)).ChainDelay(bumpDuration)
                .Chain(Tween.Scale(target, Vector3.zero, shrinkDuration, Ease.InBack)).Group(Tween.Rotation(target,
                    Quaternion.Euler(0f, 0f, 180f), shrinkDuration * 0.9f, Ease.InBack,
                    startDelay: shrinkDuration * 0.1f));

            await tcs.Task;
        }

        private async UniTask ShowRaceObject()
        {
            if (_firstGameObjectToShow != null)
                _firstGameObjectToShow.SetActive(false);

            _lastGameObjectToShow.SetActive(true);

            var t = _lastGameObjectToShow.transform;
            Vector3 originalScale = t.localScale;
            t.localScale = Vector3.zero;

            await Tween.Scale(t, Vector3.zero, originalScale * _racePopScale, _racePopDuration * 0.5f, Ease.OutBack);
            await Tween.Scale(t, originalScale * _racePopScale, originalScale, _racePopDuration * 0.5f, Ease.InBack);

            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

            _lastGameObjectToShow.SetActive(false);
        }

        private void ResetUI()
        {
            ShowCountdownImage();
            if (_lastGameObjectToShow != null)
                _lastGameObjectToShow.SetActive(false);
        }
    }
}