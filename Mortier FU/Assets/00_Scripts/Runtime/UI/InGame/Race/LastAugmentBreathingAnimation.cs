using PrimeTween;
using UnityEngine;

namespace MortierFu
{
    public class LastAugmentBreathingAnimation : MonoBehaviour
    {
        private Tween _breathTween;
        
        private void OnEnable()
        {
            if (_breathTween.isAlive)
                _breathTween.Complete();

            _breathTween = Tween.Scale(transform, Vector3.one, Vector3.one * 1.225f, 0.5f, Ease.OutQuad, cycles: -1, CycleMode.Yoyo);
        }

        private void OnDisable()
        {
            if (_breathTween.isAlive)
                _breathTween.Complete();
        }
    }
}
