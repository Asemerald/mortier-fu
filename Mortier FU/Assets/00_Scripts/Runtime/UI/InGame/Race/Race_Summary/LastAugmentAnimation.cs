using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;

namespace MortierFu
{
    public class LastAugmentAnimation : MonoBehaviour
    {
        private Tween lastAugmentTween;
        private const float scaleMultiplier = 1.255f;
        private const float durationAnimation = 1f;
        
        private void OnEnable()
        {
            if (lastAugmentTween.isAlive)
                lastAugmentTween.Stop();

            StartAugmentAnimation();
        }

        private void OnDisable()
        {
            if (lastAugmentTween.isAlive)
                lastAugmentTween.Stop();
        }

        private void StartAugmentAnimation()
        {
            lastAugmentTween = Tween.Scale(transform, Vector3.one, Vector3.one * scaleMultiplier, durationAnimation, Ease.OutQuad, cycles: -1, cycleMode:CycleMode.Yoyo);
        }
    }
}
