using System;
using UnityEngine;

namespace MortierFu
{
    public class HealthCharacterComponent : CharacterComponent
    {
        /// Sent every time health changes. Provide the amount of change (positive or negative).
        public Action<float> OnHealthChanged;
        public Action OnDeath;
        
        private int currentHealth;
        private int maxHealth;
        
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public int HealthRatio => currentHealth / maxHealth;
        public bool IsAlive => currentHealth > 0;

        public HealthCharacterComponent(PlayerCharacter playerCharacter) : base(playerCharacter)
        {
            maxHealth = 0;
            currentHealth = 0;
        }

        public override void Initialize()
        {
            maxHealth = Mathf.RoundToInt(Stats.MaxHealth.Value);
            currentHealth = maxHealth;
        }
        
        public void TakeDamage(float amount)
        {
            // Cannot take damage if already dead
            if (!IsAlive)
                return;
            
            currentHealth = Mathf.RoundToInt(Mathf.Clamp(currentHealth - amount, 0f, maxHealth));
            OnHealthChanged?.Invoke(-amount);
            
            if (!IsAlive)
            {
                currentHealth = 0;
                OnDeath?.Invoke();
            }
            
            Debug.Log($"{currentHealth} / {maxHealth}");
        }
        
        public void Heal(float amount)
        {
            currentHealth = Mathf.RoundToInt(Mathf.Clamp(currentHealth + amount, 0f, maxHealth));
            OnHealthChanged?.Invoke(amount);
        }

        public void Reset()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(maxHealth);
        }
    }
}