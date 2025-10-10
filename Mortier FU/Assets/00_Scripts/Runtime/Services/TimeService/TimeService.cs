using System.Collections.Generic;
using UnityEngine;

namespace MortierFu
{
    /// <summary>
    /// This class could be improved bu getting read of the singleton pattern
    /// and beeing instanciated by a GameManager or ServiceLocator or Injection framework.
    /// </summary>
    public class TimeService
    {
        private readonly List<TimeEffect> _activeEffects;
        private float _currentTimeScale;

        public void Update()
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                effect.ElapsedTime += Time.unscaledDeltaTime;
                
                if(effect.ElapsedTime >= effect.Duration && effect.Duration > 0f)
                {
                    _activeEffects.RemoveAt(i);
                }
            }
            
            ComputeTimeScale();
        }

        public void AddEffect(TimeEffect effect)
        {
            if (effect == null) return;

            effect.ElapsedTime = 0f; // Reset elapsed time when adding a new effect
            _activeEffects.Add(effect);
            ComputeTimeScale();
        }

        public void RemoveEffect(TimeEffect effect)
        {
            if (effect == null) return;

            if (_activeEffects.Remove(effect))
            {
                ComputeTimeScale();
            }
        }
        
        private void ComputeTimeScale()
        {
            float finalTimeScale = 1f;

            for (int i = 0; i < _activeEffects.Count; i++)
            {
                var effect = _activeEffects[i];
                
                float effectValue = effect.UseCurve 
                    ? effect.Curve.Evaluate(effect.ElapsedTime / effect.Duration) 
                    : effect.TimeScale;
                
                finalTimeScale = Mathf.Min(finalTimeScale, effectValue);
            }
            
            _currentTimeScale = finalTimeScale;
            Time.timeScale = finalTimeScale;
        }
        
        private TimeService()
        {
            _activeEffects = new List<TimeEffect>();
            ComputeTimeScale();
        }
        
        #region Lazy Singleton Pattern
        private static TimeService _instance;

        public static TimeService Instance
        {
            get
            {
                _instance ??= new TimeService();
                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            _instance = null;
        }
        
        #endregion
    }
}
