using System;
using System.Collections.Generic;
using UnityEngine;

namespace MortierFu
{
    [Serializable]
    public class Ability
    {
        [SerializeReference] public List<IEffect<PlayerCharacter>> Effects = new();

        public void Execute(PlayerCharacter target)
        {
            foreach (var effect in Effects)
            {
                if (target is PlayerCharacter playerCharacter)
                {
                    playerCharacter.ApplyEffect(effect);
                }
                else
                {
                    effect.Apply(target);
                }
            }
        }
    }

    public interface IEffect<TTarget>
    {
        void Apply(TTarget target);
        void Cancel(TTarget target);
        event Action<IEffect<TTarget>> OnCompleted;
    }

    [Serializable]
    public class DamageElementEffect : IEffect<PlayerCharacter>
    {
        public int DamageAmount = 10;

        public event Action<IEffect<PlayerCharacter>> OnCompleted;

        public void Apply(PlayerCharacter target)
        {
            target.Health.TakeDamage(DamageAmount, target);
            OnCompleted?.Invoke(this);
        }

        public void Cancel(PlayerCharacter target)
        {
            OnCompleted?.Invoke(this);
        }
    }

    [Serializable]
    public class DamageOverTimeElementEffect : IEffect<PlayerCharacter>
    {
        public float Duration = 5f;
        public float TickInterval = 1f;
        public int DamagePerTick = 1;

        private IntervalTimer _timer;
        private PlayerCharacter _currentTarget;

        public event Action<IEffect<PlayerCharacter>> OnCompleted;

        public void Apply(PlayerCharacter target)
        {
            _currentTarget = target;
            _timer = new IntervalTimer(Duration, TickInterval);

            _timer.OnInterval = OnInterval;
            _timer.OnTimerStop = OnStop;
            _timer.Start();
        }

        void OnInterval() => _currentTarget?.Health.TakeDamage(DamagePerTick, _currentTarget);
        void OnStop() => Cleanup();

        public void Cancel(PlayerCharacter target)
        {
            _timer?.Stop();
            Cleanup();
        }

        void Cleanup()
        {
            OnCompleted?.Invoke(this);
            _timer = null;
            _currentTarget = null;
        }
    }
}