using UnityEngine;
using UnityEngine.Events;

namespace MortierFu
{
    [System.Serializable]
    public class Health
    {
        /// Sent every time health changes. Provide the amount of change (positive or negative).
        public UnityAction<float> OnHealthChanged;
        public UnityAction OnDeath;
        
        [SerializeField] private float _currentHealth;
        [SerializeField] private float _maxHealth;
        
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public float HealthRatio => _currentHealth / _maxHealth;
        public bool IsAlive => _currentHealth > 0;
        
        public Health(float maxHealth)
        {
            _maxHealth = maxHealth;
            _currentHealth = maxHealth;
            OnHealthChanged = null;
            OnDeath = null;
        }
        
        public void TakeDamage(float amount)
        {
            // Cannot take damage if already dead
            if (!IsAlive) return;
            
            _currentHealth = Mathf.Clamp(_currentHealth - amount, 0f, _maxHealth);
            OnHealthChanged?.Invoke(-amount);
            
            if (!IsAlive)
            {
                _currentHealth = 0;
                OnDeath?.Invoke();
            }
        }
        
        public void Heal(float amount)
        {
            _currentHealth = Mathf.Clamp(_currentHealth + amount, 0f, _maxHealth);
            OnHealthChanged?.Invoke(amount);
        }
    }
}
