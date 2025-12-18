using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;
using PrimeTween;
using System;

namespace MortierFu
{
    public class PlayerConfirmationUI : MonoBehaviour
    {
        [Header("Player Slots (Blue, Red, Green, Yellow order)")] [SerializeField]
        private List<PlayerSlot> _playerSlots;

        [Header("Animation Settings")] [SerializeField]
        private float _pulseScale = 1.15f;

        [SerializeField] private float _pulseDuration = 0.45f;

        [SerializeField] private float _hideDuration = 0.6f;
        [SerializeField] private float _scaleDuration = 0.5f;

        [Header("References")] [SerializeField]
        private Image _countdownImage;

        [SerializeField] private GameObject _raceGameObject;

        [Header("Assets")] [SerializeField] private List<Sprite> _countdownSprites;

        [Header("Parameters")] [SerializeField]
        private float _racePopDuration = 1f;

        [SerializeField] private float _racePopScale = 1f;
        [SerializeField] private float _raceDisableDelay = 0.5f;

        [SerializeField] private Ease _aButtonEaseOut = Ease.OutBack;
        [SerializeField] private Ease _aImageEaseInOut = Ease.InOutQuad;
        [SerializeField] private Ease _playEaseIn = Ease.InBack;
        [SerializeField] private Ease _slotEaseIn = Ease.InQuint;

        private Vector3 _initialRaceScale;
        private Vector3 _initialCountdownScale;

        private Sequence _countdownSequence;

        private void Awake()
        {
            _initialCountdownScale = _countdownImage.transform.localScale;

            _initialRaceScale = _raceGameObject.transform.localScale;
            _raceGameObject.SetActive(false);

            _countdownImage.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (_countdownSequence.isAlive)
                _countdownSequence.Stop();
        }

        private async UniTask PlayCountdown(int seconds = 3)
        {
            ShowCountdownImage();

            _countdownImage.gameObject.SetActive(true);

            _countdownImage.sprite = _countdownSprites[0];

            for (int t = seconds; t > 0; t--)
            {
                SetCountdownVisual(t);
                await AnimateCountdownNumber();
            }

            _countdownImage.gameObject.SetActive(false);

            await ShowPlay();
        }

        private async UniTask ShowPlay()
        {
            _raceGameObject.SetActive(true);

            _raceGameObject.transform.localScale = Vector3.zero;

            await Tween.Scale(
                _raceGameObject.transform,
                Vector3.zero,
                _initialRaceScale,
                _racePopDuration,
                _playEaseIn
            );

            await UniTask.Delay(TimeSpan.FromSeconds(_raceDisableDelay));

            _raceGameObject.SetActive(false);
            gameObject.SetActive(false);
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

            const float growthDuration = 0.3f;
            const float shrinkDuration = 0.3f;
            float bumpDuration = 0.4f;

            _countdownSequence = Sequence.Create()
                .Chain(Tween.Scale(_countdownImage.transform, Vector3.zero, Vector3.one, growthDuration, Ease.OutBack))
                .Group(Tween.Rotation(_countdownImage.transform, Quaternion.Euler(0f, 0f, 180),
                    Quaternion.Euler(0f, 0f, 0f),
                    growthDuration * 0.9f, Ease.OutBack, startDelay: growthDuration * 0.1f))
                .ChainDelay(bumpDuration)
                .Chain(Tween.Scale(_countdownImage.transform, Vector3.zero, shrinkDuration, Ease.InBack))
                .Group(Tween.Rotation(_countdownImage.transform,
                    Quaternion.Euler(0f, 0f, 180f), shrinkDuration * 0.9f, Ease.InBack,
                    startDelay: shrinkDuration * 0.1f));

            await _countdownSequence;
        }


        private void ShowCountdownImage()
        {
            _countdownImage.transform.localScale = _initialCountdownScale;
            _countdownImage.transform.localRotation = Quaternion.identity;
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

        public void OnConfirmation()
        {
            HideConfirmation().Forget();
        }

        private async UniTaskVoid HideConfirmation()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.25f));

            foreach (var slot in _playerSlots)
            {
                if (!slot.IsActive) continue;

                if (slot.ATween.isAlive)
                    slot.ATween.Stop();
                if (slot.ScaleTween.isAlive)
                    slot.ScaleTween.Complete();

                slot.ScaleTween = Tween
                    .Scale(slot.AnimatorTransform, Vector3.one, Vector3.zero, _hideDuration, _slotEaseIn)
                    .OnComplete(() => { slot.Animator.enabled = false; });
            }

            await UniTask.Delay(TimeSpan.FromSeconds(_hideDuration));

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
                    slot.ScaleTween.Complete();

                if (!slot.IsActive) continue;

                slot.ScaleTween = Tween.Scale(slot.AnimatorTransform, Vector3.zero, Vector3.one, _scaleDuration,
                    _aButtonEaseOut);

                slot.ATween = Tween.Scale(
                    target: slot.AButtonImage.rectTransform,
                    Vector3.one * _pulseScale,
                    duration: _pulseDuration,
                    ease: _aImageEaseInOut,
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