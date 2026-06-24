using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MortierFu
{
    public sealed class LobbyJoinPromptSlot : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject _root;

        [Header("UI")]
        [SerializeField] private Image _inputImage;
        [SerializeField] private TextMeshProUGUI _label;

        [Header("Animation")]
        [SerializeField] private float _pulseFinalScale = 0.7f;
        [SerializeField] private float _pulseDuration = 0.45f;
        [SerializeField] private Ease _pulseEase = Ease.InOutQuad;

        private Tween _pulseTween;
        private bool _isVisible;
        private string _currentText;

        private Vector3 _initialInputScale;
        private Vector3 _pulseTargetScale;

        private void Awake()
        {
            if (!_root)
                _root = gameObject;

            CacheInitialScale();

            Hide();
        }

        private void OnDisable()
        {
            StopPulse();
            ResetInputScale();
        }

        private void OnDestroy()
        {
            StopPulse();
        }

        public void Show(string text)
        {
            if (_label && _currentText != text)
            {
                _currentText = text;
                _label.text = text;
            }

            if (_isVisible)
            {
                if (!_pulseTween.isAlive)
                    StartPulse();

                return;
            }

            _isVisible = true;

            if (_root)
                _root.SetActive(true);

            StartPulse();
        }

        public void Hide()
        {
            if (!_isVisible && (!_root || !_root.activeSelf))
                return;

            _isVisible = false;

            StopPulse();
            ResetInputScale();

            if (_root)
                _root.SetActive(false);
        }

        private void CacheInitialScale()
        {
            if (!_inputImage)
            {
                _initialInputScale = Vector3.one;
                _pulseTargetScale = Vector3.one * _pulseFinalScale;
                return;
            }

            _initialInputScale = _inputImage.rectTransform.localScale;

            _pulseTargetScale = new Vector3(
                _pulseFinalScale,
                _pulseFinalScale,
                _initialInputScale.z
            );
        }

        private void StartPulse()
        {
            StopPulse();

            if (!_inputImage)
                return;

            ResetInputScale();

            _pulseTween = Tween.Scale(
                target: _inputImage.rectTransform,
                endValue: _pulseTargetScale,
                duration: _pulseDuration,
                ease: _pulseEase,
                cycles: -1,
                cycleMode: CycleMode.Yoyo
            );
        }

        private void ResetInputScale()
        {
            if (!_inputImage)
                return;

            _inputImage.rectTransform.localScale = _initialInputScale;
        }

        private void StopPulse()
        {
            if (_pulseTween.isAlive)
                _pulseTween.Stop();
        }
    }
}