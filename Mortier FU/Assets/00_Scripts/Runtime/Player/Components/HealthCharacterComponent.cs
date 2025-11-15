using System;
using UnityEngine;

namespace MortierFu
{
    public class HealthCharacterComponent : CharacterComponent
    {
        /// Sent every time health changes. Provide the amount of change (positive or negative).
        public Action<float> OnHealthChanged;
        public Action<object> OnDeath;
        
        private int _currentHealth;
        private int _maxHealth;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public float HealthRatio => Mathf.Clamp01(_currentHealth / (float)_maxHealth);
        public bool IsAlive => _currentHealth > 0;

        public HealthCharacterComponent(PlayerCharacter character) : base(character)
        {
            _maxHealth = 1;
            _currentHealth = 1;
        }

        public override void Initialize() {
            Stats.MaxHealth.OnDirtyUpdated += UpdateHealth;
            UpdateHealth();
        }
        
        public void TakeDamage(int amount, object source)
        {
            // Cannot take damage if already dead
            if (!IsAlive)
                return;
            
            int previousHealth = _currentHealth;
            _currentHealth = Mathf.RoundToInt(Mathf.Clamp(_currentHealth - amount, 0f, _maxHealth));
            OnHealthChanged?.Invoke(-amount);
            
            EventBus<TriggerHealthChanged>.Raise(new TriggerHealthChanged()
            {
                Character =  Character,
                PreviousHealth = previousHealth,
                NewHealth = _currentHealth,
                MaxHealth = _maxHealth,
                Delta = -amount
            });
            
            if (!IsAlive)
            {
                _currentHealth = 0;
                OnDeath?.Invoke(source);
            }
        }
        
        public void Heal(float amount)
        {
            int previousHealth = _currentHealth;
            _currentHealth = Mathf.RoundToInt(Mathf.Clamp(_currentHealth + amount, 0f, _maxHealth));
            OnHealthChanged?.Invoke(amount);
            
            EventBus<TriggerHealthChanged>.Raise(new TriggerHealthChanged()
            {
                Character =  Character,
                PreviousHealth = previousHealth,
                NewHealth = _currentHealth,
                Delta = amount
            });
        }

        public override void Reset()
        {
            _maxHealth = Mathf.RoundToInt(Stats.MaxHealth.Value);
            _currentHealth = _maxHealth;
            OnHealthChanged?.Invoke(_maxHealth);
        }

        void UpdateHealth() {
            // Calculate the difference between the old max health and the new max health
            int newMaxHealth = Math.Max(1, Mathf.RoundToInt(Stats.MaxHealth.Value));
            int maxDelta = newMaxHealth - _maxHealth;

            _maxHealth += maxDelta;
            _currentHealth += maxDelta;
        }

        public override void Dispose() {
            Stats.MaxHealth.OnDirtyUpdated -= UpdateHealth;
        }
    }
}