using System;
using UnityEngine;
using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine.TextCore.Text;

namespace MortierFu
{
    public enum E_DeathCause
    {
        Unknown,
        BombshellExplosion,
        Fall,
        VehicleCrash,
        FallAfterExplosion
    }

    public struct DeathContext
    {
        public PlayerCharacter Killer;
        public E_DeathCause DeathCause;
        public object Source;
        public Vector3 DeathPosition;
    }

    public class HealthCharacterComponent : CharacterComponent
    {
        private readonly HashSet<object> _invincibilitySources = new();

        private float _currentHealth;
        private float _maxHealth;
        private float _lastBombShellDamageTime;
        
        private PlayerCharacter _lastDamageCause; 
        
        public Action<object> OnDeath;

        /// Sent every time health changes. Provide the old health and the new health.
        public Action<float, float> OnHealthChanged;

        public Action<float> OnMaxHealthChanged;
        public Action<bool> OnInvincibilityChanged;

        private bool IsInvincible => _invincibilitySources.Count > 0;

        public HealthCharacterComponent(PlayerCharacter character) : base(character)
        {
            _maxHealth = 1f;
            _currentHealth = 1f;
        }

        public float MaxHealth => _maxHealth;
        public float HealthRatio => Mathf.Clamp01(_currentHealth / _maxHealth);
        public bool IsAlive => _currentHealth > 0f;

        public override void Initialize()
        {
            Stats.MaxHealth.OnDirtyUpdated += UpdateHealth;
            _lastDamageCause = null;
            _lastBombShellDamageTime = float.NegativeInfinity;
            UpdateHealth();
        }

        public bool TakeDamage(float amount, object source, bool isLethal = false, bool ignoreInvincibility = false)
        {
            if (!IsAlive)
                return false;

            if (IsInvincible && !ignoreInvincibility)
                return false;

            var damageAmount = isLethal ? _currentHealth : amount;

            if (damageAmount <= 0f)
                return false;
            
            

            var previousHealth = _currentHealth;
            _currentHealth = Mathf.Clamp(_currentHealth - damageAmount, 0f, _maxHealth);

            OnHealthChanged?.Invoke(previousHealth, _currentHealth);

            Character.Aspect?.PlayDamageBlink(
                blinkColor: Color.white,
                blinkCount: 5,
                blinkDuration: 0.08f
            );

            if (!isLethal)
            {
                _lastBombShellDamageTime = Time.time;
                _lastDamageCause = source as PlayerCharacter;
            }

            EventBus<TriggerHealthChanged>.Raise(new TriggerHealthChanged()
            {
                Instigator = source as PlayerCharacter,
                Character = Character,
                PreviousHealth = previousHealth,
                NewHealth = _currentHealth,
                MaxHealth = _maxHealth,
                Delta = -damageAmount
            });

            character.ShakeService.ShakeController(character.Owner, ShakeService.ShakeType.BIG);

            if (IsAlive) return true;

            _currentHealth = 0;
            OnDeath?.Invoke(source);

            DeathContext context = ResolveDeathContext(character, source);

            EventBus<EventPlayerDeath>.Raise(new EventPlayerDeath
            {
                Character = Character,
                Context = context
            });

            return true;
        }

        private DeathContext ResolveDeathContext(PlayerCharacter character, object source)
        {
            // If killed by a player's bombshell
            if (source is PlayerCharacter killer)
            {
                AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Player_Death, character.transform.position);
                return CreateDeathContext(character, source, killer, E_DeathCause.BombshellExplosion);
            }

            // Died by falling
            if (source is DeathTrigger)
            {
                var kn = character.KnockbackState;
                
                // Only consider recent push
                if (kn.ComputeLastBumpElapsedTime() < 4f)
                {
                    if (kn.LastBumpSource is Bumper)
                    {
                        AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Player_CarCrash, character.transform.position);
                        return CreateDeathContext(character, source, kn.LastPusher, E_DeathCause.VehicleCrash);
                    }

                    if (kn.LastPusher)
                    {
                        AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Player_Death, character.transform.position);
                        return CreateDeathContext(character, source, kn.LastPusher, E_DeathCause.Fall);
                    }
                }

                if (Time.time - _lastBombShellDamageTime < 4f)
                {
                    return CreateDeathContext(character, source, _lastDamageCause , E_DeathCause.FallAfterExplosion);
                }
            }

            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Player_Fall, character.transform.position);
            return CreateDeathContext(character, source, null, E_DeathCause.Fall);
        }

        private DeathContext CreateDeathContext(PlayerCharacter character, object source, PlayerCharacter killer, E_DeathCause deathCause)
        {
            Vector3 deathPosition = character.FeetPoint ? character.FeetPoint.position : character.transform.position;

            return new DeathContext
            {
                Killer = killer,
                DeathCause = deathCause,
                Source = source,
                DeathPosition = deathPosition
            };
        }

        public bool TakeLethalDamage(object source, bool ignoreInvincibility = false)
        {
            return TakeDamage(_currentHealth, source, isLethal: true, ignoreInvincibility: ignoreInvincibility);
        }

        public bool Heal(float amount, object source = null, bool allowRevive = false)
        {
            if (amount <= 0f)
                return false;

            if (!IsAlive && !allowRevive)
                return false;

            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Clamp(_currentHealth + amount, 0f, _maxHealth);

            if (Mathf.Approximately(previousHealth, _currentHealth))
                return false;

            OnHealthChanged?.Invoke(previousHealth, _currentHealth);

            EventBus<TriggerHealthChanged>.Raise(new TriggerHealthChanged
            {
                Instigator = source as PlayerCharacter,
                Character = Character,
                PreviousHealth = previousHealth,
                NewHealth = _currentHealth,
                MaxHealth = _maxHealth,
                Delta = _currentHealth - previousHealth
            });

            return true;
        }
        
        public bool Revive(float healthAmount, object source = null)
        {
            return !IsAlive && Heal(healthAmount, source, allowRevive: true);
        }

        public override void Reset()
        {
            var previousHealth = _currentHealth;
            _currentHealth = _maxHealth;
            _lastDamageCause = null;
            _lastBombShellDamageTime = float.NegativeInfinity;
            OnHealthChanged?.Invoke(previousHealth, _currentHealth);
        }

        void UpdateHealth()
        {
            var newMaxHealth = Stats.MaxHealth.Value;

            var delta = newMaxHealth - _maxHealth;

            _maxHealth = newMaxHealth;

            if (delta > 0)
            {
                _currentHealth += delta;
            }

            var previousHealth = _currentHealth;
            _currentHealth = Mathf.Clamp(_currentHealth, 0f, _maxHealth);

            OnHealthChanged?.Invoke(previousHealth, _currentHealth);
            OnMaxHealthChanged?.Invoke(_maxHealth);
        }

        public void AddInvincibility(object source)
        {
            if (source is null)
                return;

            var wasInvincible = IsInvincible;

            if (!_invincibilitySources.Add(source))
                return;

            if (wasInvincible != IsInvincible)
                OnInvincibilityChanged?.Invoke(IsInvincible);
        }

        public void RemoveInvincibility(object source)
        {
            if (source is null)
                return;

            var wasInvincible = IsInvincible;

            if (!_invincibilitySources.Remove(source))
                return;

            if (wasInvincible != IsInvincible)
                OnInvincibilityChanged?.Invoke(IsInvincible);
        }

        public void ClearInvincibility()
        {
            if (_invincibilitySources.Count == 0)
                return;

            _invincibilitySources.Clear();
            OnInvincibilityChanged?.Invoke(false);
        }

        public override void Dispose()
        {
            Stats.MaxHealth.OnDirtyUpdated -= UpdateHealth;
        }
        
        public void RefreshFromStats()
        {
            UpdateHealth();
        }
    }
}