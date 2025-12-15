using System.Collections.Generic;
using System.Linq;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class StrikeState : BaseState
    {
        private Collider[] _overlapBuffer = new Collider[100];
        
        private CountdownTimer _strikeCooldownTimer;
        private CountdownTimer _strikeTriggerTimer;

        public StrikeState(PlayerCharacter character, Animator animator) : base(character, animator)
        {
            _strikeCooldownTimer = new CountdownTimer(character.Stats.StrikeCooldown.Value);
            _strikeTriggerTimer = new CountdownTimer(character.Stats.StrikeDuration.Value);

            character.Stats.StrikeCooldown.OnDirtyUpdated += UpdateStrikeCooldown;
            character.Stats.StrikeDuration.OnDirtyUpdated += UpdateStrikeDuration;
        }

        public bool InCooldown => _strikeCooldownTimer.IsRunning;
        public bool IsFinished => _strikeTriggerTimer.IsFinished;
        
        public float StrikeCooldownProgress => _strikeCooldownTimer.Progress;
        
        
        public override void OnEnter()
        {
            _strikeCooldownTimer.Start();
            _strikeTriggerTimer.Start();

            EventBus<TriggerStrike>.Raise(new TriggerStrike()
            {
                Character =  character,
            });
            
            TEMP_FXHandler.Instance.InstantiateStrikeFX(character.transform, character.Stats.GetStrikeRadius());
            if(debug)
                Logs.Log("Entering Strike State");
        }

        public override void Update()
        {
            character.Controller.HandleMovementUpdate(0.2f);
        }

        public override void FixedUpdate()
        {
            character.Controller.HandleMovementFixedUpdate();
            
            ExecuteStrike();
        }

        public override void OnExit()
        {
            _strikeTriggerTimer.Stop();
            
            if(debug) 
                Logs.Log("Exiting Strike State");
        }

        public void Reset() {
            _strikeCooldownTimer.Reset();
            _strikeCooldownTimer.Stop();
            
            _strikeTriggerTimer.Reset();
            _strikeTriggerTimer.Stop();
        }
        
        private void ExecuteStrike()
        {
            var origin = character.transform.position;
            var count = Physics.OverlapSphereNonAlloc(origin, character.Stats.GetStrikeRadius(), _overlapBuffer);

            // Pour éviter de détecter plusieurs fois les mêmes objets ou joueurs
            var processedRoots = new HashSet<GameObject>();
            var hitCharacters = new HashSet<PlayerCharacter>();
            var blockedBombshells = new HashSet<Bombshell>();
            
            for (var i = 0; i < count; i++)
            {
                var hit = _overlapBuffer[i];
                if (hit == null) continue;
                 
                var root = hit.transform.root.gameObject;
                 
                // TEMP ajout breakable
                if (hit.TryGetComponent(out IInteractable interactable) && interactable.IsStrikeInteractable)
                {
                    interactable.Interact();
                    continue;
                }
                
                if (!processedRoots.Add(root)) continue;

                if (hit.TryGetComponent(out Bombshell bombshell))
                {
                    blockedBombshells.Add(bombshell);
                    bombshell.ReturnToPool();

                    continue;
                }

                var other = hit.GetComponentInParent<PlayerCharacter>();
                if (other == null) continue;
                if (other == character) continue;

                hitCharacters.Add(other);
                
                int strikeDamage = Mathf.RoundToInt(character.Stats.StrikeDamage.Value);
                other.Health.TakeDamage(strikeDamage, character);

                var knockbackDir = (other.transform.position - character.transform.position).normalized;
                var knockbackForce = knockbackDir * character.Stats.GetStrikePushForce();
                float knockbackDuration = character.Stats.KnockbackDuration.Value;
                float stunDuration = character.Stats.GetKnockbackStunDuration();
                other.ReceiveKnockback(knockbackDuration, knockbackForce, stunDuration);
            }

            if (hitCharacters.Count > 0)
            {
                EventBus<TriggerStrikeHit>.Raise(new TriggerStrikeHit()
                {
                    Character =  character,
                    HitCharacters = hitCharacters.ToArray(),
                });   
            }

            if (blockedBombshells.Count > 0)
            {
                EventBus<TriggerStrikeHitBombshell>.Raise(new TriggerStrikeHitBombshell()
                {
                    Character = character,
                    HitBombshells = blockedBombshells.ToArray(),
                });
            }
        }

        private void UpdateStrikeCooldown() {
            float strikeCooldown = character.Stats.StrikeCooldown.Value;
            _strikeCooldownTimer.DynamicUpdate(strikeCooldown);
        }

        private void UpdateStrikeDuration() {
            float strikeDuration = character.Stats.StrikeDuration.Value;
            _strikeTriggerTimer.DynamicUpdate(strikeDuration);
        }

        public override void Dispose() {
            character.Stats.StrikeCooldown.OnDirtyUpdated -= UpdateStrikeCooldown;
            character.Stats.StrikeDuration.OnDirtyUpdated -= UpdateStrikeDuration;
        }
    }
}