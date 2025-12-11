using MortierFu.Shared;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace MortierFu
{
    public class RacePressureUI : MonoBehaviour
    {
        [SerializeField] private Image _vignetteImage;

        private AugmentSelectionSystem _augmentSelectionSystem;

        private Tween _vignetteTween;
        
        private void Start()
        {
            _augmentSelectionSystem = SystemManager.Instance.Get<AugmentSelectionSystem>();

            if (_augmentSelectionSystem == null)
            {
                Debug.LogError($"[RacePressureUI] No AugmentSelectionSystem found for {gameObject.name}");
                return;
            }

            _augmentSelectionSystem.OnPressureStart += StartVignettePressure;
            _augmentSelectionSystem.OnPressureStop += StopVignettePressure;
        }

        // Safe to unsubscribe because systems are disposed after the map is unloaded.
        void OnDestroy()
        {
            if (_augmentSelectionSystem == null)
            {
                Logs.LogWarning("[RacePressureUI] No AugmentSelectionSystem found for {gameObject.name}: Potential memory leak.");
                return;
            }
            
            _augmentSelectionSystem.OnPressureStart -= StartVignettePressure;
            _augmentSelectionSystem.OnPressureStop -= StopVignettePressure;
        }

        private void StartVignettePressure(float duration)
        {
            if (_vignetteImage == null)
                return;

            _vignetteImage.gameObject.SetActive(true);
            if (_vignetteTween.isAlive)
                _vignetteTween.Stop();

            var color = _vignetteImage.color;
            color.a = 0f;
            _vignetteImage.color = color;

            _vignetteTween = Tween.Custom(0f, 1f, 0.6f,
                a =>
                {
                    var c = _vignetteImage.color;
                    c.a = a;
                    _vignetteImage.color = c;
                },
                cycles: -1,
                cycleMode: CycleMode.Yoyo
            );

            Tween.Delay(duration, StopVignettePressure);
        }

        private void StopVignettePressure()
        {
            if (_vignetteTween.isAlive)
                _vignetteTween.Stop();

            if (_vignetteImage != null)
                _vignetteImage.gameObject.SetActive(false);
        }
    }
}