using PrimeTween;
using TMPro;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbyJoinPromptSlot : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject _root;

        [Header("UI")]
        [SerializeField] private RectTransform _inputImage;
        [SerializeField] private TextMeshProUGUI _label;

        [Header("Animation")]
        [SerializeField] private Vector3 _pulseInitScale = Vector3.one;
        [SerializeField] private Vector3 _pulseTargetScale = Vector3.one * 0.7f;
        [SerializeField] private float _pulseDuration = 0.6f;
        [SerializeField] private Ease _pulseEase = Ease.InOutQuad;

        private Tween _pulseTween;
        private bool _isVisible;
        private string _currentText;

        private void Awake()
        {
            if (!_root)
                _root = gameObject;

            Hide();
        }

        private void OnDisable()
        {
            _isVisible = false;

            StopPulse();
            ResetInputScale();
        }

        private void OnDestroy() => StopPulse();

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

        private void StartPulse()
        {
            StopPulse();

            if (!_inputImage)
                return;

            if (!_inputImage.gameObject.activeInHierarchy)
                return;

            ResetInputScale();

            _pulseTween = Tween.Scale(target: _inputImage, endValue: _pulseTargetScale, duration: _pulseDuration, ease: _pulseEase, cycles: -1, cycleMode: CycleMode.Yoyo);
        }

        private void ResetInputScale()
        {
            if (!_inputImage)
                return;

            _inputImage.localScale = _pulseInitScale;
        }

        private void StopPulse()
        {
            if (_pulseTween.isAlive)
                _pulseTween.Stop();
        }
    }
}