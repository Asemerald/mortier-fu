using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;

namespace MortierFu
{
    public class LastAugmentAnimation : MonoBehaviour
    {
        private Tween _lastAugmentTween;
        private const float ScaleMultiplier = 1.255f;
        private const float DurationAnimation = 1f;
        private readonly Vector3 _originSize = new Vector3(0.8f, 0.8f, 0.8f);
        
        private void OnEnable()
        {
            if (_lastAugmentTween.isAlive)
                _lastAugmentTween.Stop();
            
            StartAugmentAnimation();
        }

        private void OnDisable()
        {
            if (_lastAugmentTween.isAlive)
                _lastAugmentTween.Stop();
        }

        private void StartAugmentAnimation()
        {
            _lastAugmentTween = Tween.Scale(transform, _originSize, Vector3.one, DurationAnimation, Ease.OutQuad, cycles: -1, cycleMode:CycleMode.Yoyo);
        }
    }
}
