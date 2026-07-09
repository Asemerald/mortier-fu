using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;

namespace MortierFu
{
    public class LastAugmentAnimation : MonoBehaviour
    {
        private Tween lastAugmentTween;
        private const float scaleMultiplier = 1.5f;
        private const float durationAnimation = 1f;
        
        private void OnEnable()
        {
            if (lastAugmentTween.isAlive)
                lastAugmentTween.Stop();
            
            StartAugmentAnimation().Forget();
        }

        private void OnDisable()
        {
            if (lastAugmentTween.isAlive)
                lastAugmentTween.Stop();
        }

        private async UniTaskVoid StartAugmentAnimation()
        {
            lastAugmentTween = Tween.Scale(transform, Vector3.one, Vector3.one * scaleMultiplier, durationAnimation, Ease.OutQuad);
            await lastAugmentTween;
        }
    }
}
