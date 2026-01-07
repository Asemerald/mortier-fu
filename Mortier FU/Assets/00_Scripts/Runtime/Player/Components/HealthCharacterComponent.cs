using System;
using JetBrains.Annotations;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class HealthCharacterComponent : CharacterComponent
    {
        /// Sent every time health changes. Provide the amount of change (positive or negative).
        public Action<float> OnHealthChanged;

        public Action<object> OnDeath;

        private float _currentHealth;
        private float _maxHealth;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public float HealthRatio => Mathf.Clamp01(_currentHealth / _maxHealth);
        public bool IsAlive => _currentHealth > 0f;

        public HealthCharacterComponent(PlayerCharacter character) : base(character)
        {
            _maxHealth = 1f;
            _currentHealth = 1f;
        }

        public override void Initialize()
        {
            Stats.MaxHealth.OnDirtyUpdated += UpdateHealth;
            UpdateHealth();
        }

        public void TakeDamage(float amount, object source, bool isLethal = false)
        {
            // Cannot take damage if already dead
            if (!IsAlive)
                return;

            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Clamp(_currentHealth - amount, 0f, _maxHealth);
            OnHealthChanged?.Invoke(-amount);

            Character.Aspect?.PlayDamageBlink(
                blinkColor: Color.white,
                blinkCount: 5,
                blinkDuration: 0.15f
            );
            
            EventBus<TriggerHealthChanged>.Raise(new TriggerHealthChanged()
            {
                Instigator = source as PlayerCharacter, // Si ça ça marche jsuis content
                Character = Character,
                PreviousHealth = previousHealth,
                NewHealth = _currentHealth,
                MaxHealth = _maxHealth,
                Delta = -amount
            });

            if (!IsAlive)
            {
                _currentHealth = 0;
                OnDeath?.Invoke(source);
                
                EventBus<EventPlayerDeath>.Raise(new EventPlayerDeath() {
                    Character = Character,
                    Source = source
                });
            }
        }

        public void TakeLethalDamage(object source) => TakeDamage(_currentHealth, source, true);

        public void Heal(float amount)
        {
            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Clamp(_currentHealth + amount, 0f, _maxHealth);
            OnHealthChanged?.Invoke(amount);

            EventBus<TriggerHealthChanged>.Raise(new TriggerHealthChanged()
            {
                Instigator = null,
                Character = Character,
                PreviousHealth = previousHealth,
                NewHealth = _currentHealth,
                Delta = amount
            });
        }

        public override void Reset()
        {
            _currentHealth = _maxHealth;
            OnHealthChanged?.Invoke(_maxHealth);
        }

        void UpdateHealth()
        {
            float newMaxHealth = Mathf.Max(1f, Stats.MaxHealth.Value);

            // Calculate gain or loss in max health
            float delta = newMaxHealth - _maxHealth;

            _maxHealth = newMaxHealth;

            // If max health increased, add the same amount to current health
            if (delta > 0)
            {
                _currentHealth += delta;
            }

            // Always clamp to ensure we stay inside valid bounds
            _currentHealth = Mathf.Clamp(_currentHealth, 0f, _maxHealth);
        }

        public override void Dispose()
        {
            Stats.MaxHealth.OnDirtyUpdated -= UpdateHealth;
        }
    }
}