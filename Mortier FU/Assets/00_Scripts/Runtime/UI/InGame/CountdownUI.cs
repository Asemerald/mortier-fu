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

        [SerializeField] private GameObject _lastGameObjectToShow;

        [Header("Assets")] [SerializeField] private List<Sprite> _countdownSprites;

        [Header("Parameters")] [SerializeField]
        private float _racePopDuration = 1f;

        [SerializeField] private float _racePopScale = 1.2f;

        private Vector3 _initialRaceScale;
        private Vector3 _initialCountdownScale;

        private Sequence _countdownSequence;

        private void Awake()
        {
            _initialCountdownScale = _countdownImage.transform.localScale;

            if (_lastGameObjectToShow != null)
            {
                _initialRaceScale = _lastGameObjectToShow.transform.localScale;
                _lastGameObjectToShow.SetActive(false);
            }

            _countdownImage.gameObject.SetActive(false);
        }

        public async UniTask PlayCountdown(int seconds = 3)
        {
            ResetUI();
            ShowCountdownImage();

            for (int t = seconds; t > 0; t--)
            {
                SetCountdownVisual(t);
                AnimateCountdownNumber().Forget();
                await UniTask.Delay(TimeSpan.FromSeconds(1));
            }

            _countdownImage.gameObject.SetActive(false);

            if (_lastGameObjectToShow != null)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1));
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
            Vector3 targetScale = Vector3.one;

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
            _lastGameObjectToShow.SetActive(true);

            var t = _lastGameObjectToShow.transform;
            t.localScale = Vector3.zero;

            await Tween.Scale(
                t,
                t.localScale * _racePopScale,
                _initialRaceScale,
                _racePopDuration * 0.5f,
                Ease.InBack
            );

            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

            t.localScale = _initialRaceScale;
            _lastGameObjectToShow.SetActive(false);
            gameObject.SetActive(false);
        }

        private void ResetUI()
        {
            ShowCountdownImage();
            if (_lastGameObjectToShow != null)
                _lastGameObjectToShow.SetActive(false);
        }
    }
}