using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace MortierFu
{
    public class RacePressureUI : MonoBehaviour
    {
        [Header("Parameters"), SerializeField] private float _pulseDuration = 0.6f;

        [Header("References"), SerializeField] private Image _vignetteImage;

        private Tween _vignetteTween;
        private Tween _delayTween;

        private Color _baseColor;
        private Color _tempColor;

        private void Start()
        {
            _baseColor = _vignetteImage.color;
        }

        public void StartVignettePressure(float duration)
        {
            if (_vignetteImage == null)
                return;

            if (_vignetteTween.isAlive)
                _vignetteTween.Stop();

            if (_delayTween.isAlive)
                _delayTween.Stop();

            _vignetteImage.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0f);
            _vignetteImage.gameObject.SetActive(true);

            _vignetteTween = Tween.Custom(
                startValue: 0f,
                endValue: 1f,
                duration: _pulseDuration,
                a =>
                {
                    _tempColor = _baseColor;
                    _tempColor.a = a;
                    _vignetteImage.color = _tempColor;
                },
                cycles: -1,
                cycleMode: CycleMode.Yoyo
            );
            _delayTween = Tween.Delay(duration, StopVignettePressure);
        }

        public void StopVignettePressure()
        {
            if (_vignetteTween.isAlive)
                _vignetteTween.Stop();

            if (_delayTween.isAlive)
                _delayTween.Stop();

            if (_vignetteImage == null) return;

            _tempColor = _baseColor;
            _tempColor.a = 0f;
            _vignetteImage.color = _tempColor;
            _vignetteImage.gameObject.SetActive(false);
        }
    }
}