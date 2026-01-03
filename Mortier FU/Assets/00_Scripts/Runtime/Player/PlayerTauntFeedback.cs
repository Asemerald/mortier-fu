using System;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace MortierFu
{
    public class PlayerTauntFeedback : MonoBehaviour
    {
        #region Variables

        #region References

        [Header("Character Reference")] [SerializeField]
        private PlayerCharacter _character;

        [Header("UI Elements")] [SerializeField]
        private Image _tauntImg;

        [SerializeField] private Sprite[] _characterIcons;

        [Header("Audio")] [SerializeField] private AudioSource _tauntAudioSource;
        [SerializeField] private AudioClip _tauntAudioClip;

        [Header("Tween Settings")] [SerializeField]
        private Ease _scaleInEase = Ease.OutBack;

        [SerializeField] private Ease _scaleOutEase = Ease.InBack;

        [SerializeField] private float _scaleInDuration = 0.5f;
        [SerializeField] private float _holdDuration = 0.5f;
        [SerializeField] private float _scaleOutDuration = 0.3f;

        #endregion

        #region Private Fields

        private Tween _currentTween;
        private bool _isTaunting;

        #endregion

        #endregion

        #region Unity Callbacks

        private void Start()
        {
            if (_tauntImg == null || _character == null) return;

            _tauntImg.sprite = _characterIcons[_character.Owner.PlayerIndex];
            _tauntImg.enabled = false;
        }

        #endregion

        #region Taunt

        public async UniTask PlayTauntAsync()
        {
            if (_isTaunting) return;

            _isTaunting = true;

            if (_currentTween.isAlive)
                _currentTween.Stop();

            await UniTask.Yield();

            _tauntImg.transform.localScale = Vector3.zero;
            _tauntImg.enabled = true;

            _currentTween = Tween.Scale(_tauntImg.transform, 1f, _scaleInDuration, _scaleInEase);
            await _currentTween;

            _tauntAudioSource?.PlayOneShot(_tauntAudioClip);

            await UniTask.Delay(TimeSpan.FromSeconds(_holdDuration));

            _currentTween = Tween.Scale(_tauntImg.transform, 0f, _scaleOutDuration, _scaleOutEase);
            await _currentTween;

            _tauntImg.enabled = false;
            _isTaunting = false;
        }

        #endregion
    }
}