using System;
using System.Collections.Generic;
using UnityEngine;

namespace MortierFu
{
    [Serializable]
    public class Ability
    {
        [SerializeReference] public List<IEffect<PlayerCharacter>> Effects = new();

        public void Execute(PlayerCharacter target, PlayerCharacter owner)
        {
            foreach(var effect in Effects)
                effect.Apply(target, owner);
        }
    }
    
    public interface IEffect<TTarget>
    {
        void Apply(TTarget target, TTarget owner);
        void Cancel();
    }

    [Serializable]
    public class DamageElementEffect : IEffect<PlayerCharacter>
    {
        public int DamageAmount = 10;

        public void Apply(PlayerCharacter target, PlayerCharacter owner)
        {
            target.Health.TakeDamage(DamageAmount, owner);
        }

        public void Cancel()
        {
            // noop
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
        private PlayerCharacter _owner;

        public void Apply(PlayerCharacter target, PlayerCharacter owner)
        {
            _currentTarget = target;
            _owner = owner;
            _timer = new IntervalTimer(Duration, TickInterval);
            
            _timer.OnInterval = OnInterval;
            _timer.OnTimerStop = OnStop;
            _timer.Start();
        }

        void OnInterval() => _currentTarget?.Health.TakeDamage(DamagePerTick, _owner);
        void OnStop() => Cleanup();

        public void Cancel()
        {
            _timer?.Stop();
            Cleanup();
        }

        void Cleanup()
        {
            _timer = null;
            _currentTarget = null;
        }
    }
}
