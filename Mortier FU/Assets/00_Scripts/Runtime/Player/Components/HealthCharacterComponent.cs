using System;
using JetBrains.Annotations;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public enum E_DeathCause
    {
        Unknown,
        BombshellExplosion,
        Fall,
        VehicleCrash
    }

    public struct DeathContext
    {
        public PlayerCharacter Killer;
        public E_DeathCause DeathCause;
    }

    public class HealthCharacterComponent : CharacterComponent
    {
        private float _currentHealth;
        private float _maxHealth;

        public Action<object> OnDeath;

        /// Sent every time health changes. Provide the old health and the new health.
        public Action<float, float> OnHealthChanged;

        public Action<float> OnMaxHealthChanged;

        public HealthCharacterComponent(PlayerCharacter character) : base(character)
        {
            _maxHealth = 1f;
            _currentHealth = 1f;
        }

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public float HealthRatio => Mathf.Clamp01(_currentHealth / _maxHealth);
        public bool IsAlive => _currentHealth > 0f;

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
            OnHealthChanged?.Invoke(previousHealth, _currentHealth);

            Character.Aspect?.PlayDamageBlink(
                blinkColor: Color.white,
                blinkCount: 5,
                blinkDuration: 0.08f
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

            character.ShakeService.ShakeController(character.Owner, ShakeService.ShakeType.BIG);

            if (!IsAlive)
            {
                _currentHealth = 0;
                OnDeath?.Invoke(source);

                EventBus<EventPlayerDeath>.Raise(new EventPlayerDeath()
                {
                    Character = Character,
                    Context = ResolveDeathContext(character, source)
                });

                //TODO : placeholder
                if (isLethal) AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Player_Fall, character.transform.position);
                else AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Player_Death, character.transform.position);
            }
        }

        private DeathContext ResolveDeathContext(PlayerCharacter character, object source)
        {
            // If killed by a player's bombshell
            if (source is PlayerCharacter killer)
            {
                AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Player_Death, character.transform.position);

                return new DeathContext
                {
                    Killer = killer,
                    DeathCause = E_DeathCause.BombshellExplosion
                };
            }

            // Died by falling
            if (source is DeathTrigger)
            {
                var kn = character.KnockbackState;

                // Only consider recent push
                if (kn.ComputeLastBumpElapsedTime() < 8f)
                {
                    if (kn.LastBumpSource is Bumper bumper)
                    {
                        AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Player_CarCrash,
                            character.transform.position);

                        return new DeathContext
                        {
                            Killer = kn.LastPusher,
                            DeathCause = E_DeathCause.VehicleCrash
                        };
                    }

                    if (kn.LastPusher)
                    {
                        AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Player_Death,
                            character.transform.position);

                        return new DeathContext
                        {
                            Killer = kn.LastPusher,
                            DeathCause = E_DeathCause.Fall
                        };
                    }
                }
            }

            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Player_Fall, character.transform.position);

            return new DeathContext
            {
                Killer = null,
                DeathCause = E_DeathCause.Fall
            };
        }

        public void TakeLethalDamage(object source) => TakeDamage(_currentHealth, source, true);

        public void Heal(float amount)
        {
            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Clamp(_currentHealth + amount, 0f, _maxHealth);
            OnHealthChanged?.Invoke(previousHealth, _currentHealth);

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
            float previousHealth = _currentHealth;
            _currentHealth = _maxHealth;
            OnHealthChanged?.Invoke(previousHealth, _currentHealth);
        }

        void UpdateHealth()
        {
            float newMaxHealth = Stats.MaxHealth.Value;

            // Calculate gain or loss in max health
            float delta = newMaxHealth - _maxHealth;

            _maxHealth = newMaxHealth;

            // If max health increased, add the same amount to current health
            if (delta > 0)
            {
                _currentHealth += delta;
            }

            // Always clamp to ensure we stay inside valid bounds
            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Clamp(_currentHealth, 0f, _maxHealth);

            OnHealthChanged?.Invoke(previousHealth, _currentHealth);
            OnMaxHealthChanged?.Invoke(_maxHealth);
        }

        public override void Dispose()
        {
            Stats.MaxHealth.OnDirtyUpdated -= UpdateHealth;
        }
    }
}