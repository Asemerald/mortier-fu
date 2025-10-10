using UnityEngine;

namespace MortierFu
{
    [System.Serializable]
    public class TimeEffect //, IDisposable?
    {
        [Tooltip("If true, use the curve to evaluate the time scale over time")]
        public bool UseCurve = true;
        [Tooltip("Absolute time scale value over relative time [0,1]")]
        public AnimationCurve Curve;
        [Tooltip("Used in case no curve is provided")]
        public float TimeScale = 1f;
        
        public float Duration;
        public float ElapsedTime;
        
        public TimeEffect(float timeScale, float duration = -1f, AnimationCurve curve = null)
        {
            TimeScale = timeScale;
            Duration = duration;
            ElapsedTime = 0f;
            Curve = curve;
        }
    }
}