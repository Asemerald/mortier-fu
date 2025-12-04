using System.Collections.Generic;
using UnityEngine;
using System;

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

        public void Cancel(PlayerCharacter target)
        {
            foreach (var effect in Effects)
            {
                target.RemoveEffect(effect);
                effect.Cancel(target);
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
        private PlayerCharacter _currentTarget;

        private IntervalTimer _timer;

        public event Action<IEffect<PlayerCharacter>> OnCompleted;

        public void Apply(PlayerCharacter target)
        {
            _currentTarget = target;
            
            if (_timer is { IsRunning: true }) return;
            
            _timer = new IntervalTimer(Duration, TickInterval);

            _timer.OnInterval = OnInterval;
            _timer.OnTimerStop = OnStop;
            _timer.Start();
        }

        public void Cancel(PlayerCharacter target)
        {
            _timer?.Stop();
            Cleanup();
        }

        void OnInterval() => _currentTarget?.Health.TakeDamage(DamagePerTick, this); // TODO: get owner
        void OnStop() => Cleanup();

        void Cleanup()
        {
            OnCompleted?.Invoke(this);
            _timer = null;
            _currentTarget = null;
        }
    }
}