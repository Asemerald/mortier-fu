using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MortierFu.Shared
{
    public sealed class SmoothedMultiplier
    {
        public float Value { get; private set; } = 1f;
 
        private CancellationTokenSource _cts;
 
        public void SetTarget(float target, float transitionDuration, MonoBehaviour owner)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(owner.GetCancellationTokenOnDestroy());
 
            SmoothTransition(target, transitionDuration, _cts.Token).Forget();
        }
 
        private async UniTaskVoid SmoothTransition(float target, float duration, CancellationToken token)
        {
            float start = Value;
 
            if (duration <= 0f)
            {
                Value = target;
                return;
            }
 
            float elapsed = 0f;
 
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                Value = Mathf.Lerp(start, target, elapsed / duration);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
 
            Value = target;
        }

        public void Reset() => Value = 1f;
    }
}