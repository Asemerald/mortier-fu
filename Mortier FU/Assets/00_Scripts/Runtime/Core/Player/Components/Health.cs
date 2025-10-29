using System;
using UnityEngine;
using UnityEngine.Events;

namespace MortierFu
{
    [Serializable]
    public class Health
    {
        /// Sent every time health changes. Provide the amount of change (positive or negative).
        public UnityAction<float> OnHealthChanged;
        public UnityAction OnDeath;
        
        [SerializeField] private float _currentHealth;
        [SerializeField] private float _maxHealth;
        private SO_CharacterStats _characterStats;
        
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public float HealthRatio => _currentHealth / _maxHealth;
        public bool IsAlive => _currentHealth > 0;

        public event Action<Health, Health> OnDeathEvent = delegate { };
        
        public Health(SO_CharacterStats characterStats)
        {
            _characterStats = characterStats;

            _maxHealth = characterStats.MaxHealth.Value;
            _currentHealth = _maxHealth;
            OnHealthChanged = null;
            OnDeath = null;
        }

        public void TakeDamage(float amount)
        {
            // Cannot take damage if already dead
            if (!IsAlive)
                return;
            
            _currentHealth = Mathf.Clamp(_currentHealth - amount, 0f, _maxHealth);
            OnHealthChanged?.Invoke(-amount);
            
            if (!IsAlive)
            {
                _currentHealth = 0;
                OnDeath?.Invoke();
            }
            
            Debug.Log($"{_currentHealth} / {_maxHealth}");
        }
        
        public void Heal(float amount)
        {
            _currentHealth = Mathf.Clamp(_currentHealth + amount, 0f, _maxHealth);
            OnHealthChanged?.Invoke(amount);
        }

        public void Reset()
        {
            _currentHealth = _maxHealth;
            OnHealthChanged?.Invoke(_maxHealth);
        }
    }
}