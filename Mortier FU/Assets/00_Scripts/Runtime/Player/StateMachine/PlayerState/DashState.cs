using System.Collections.Generic;
using System.Linq;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class DashState : BaseState
    {
        private Collider[] _overlapBuffer = new Collider[100];

        private CountdownTimer _dashCooldownTimer;
        private CountdownTimer _dashTriggerTimer;

        public DashState(PlayerCharacter character, Animator animator) : base(character, animator)
        {
            _dashCooldownTimer = new CountdownTimer(character.Stats.GetDashCooldown());
            _dashTriggerTimer = new CountdownTimer(character.Stats.DashDuration.Value);

            character.Stats.DashCooldown.OnDirtyUpdated += UpdateDashCooldown;
            character.Stats.DashDuration.OnDirtyUpdated += UpdateDashDuration;
        }

        public bool InCooldown => _dashCooldownTimer.IsRunning;
        public bool IsFinished => _dashTriggerTimer.IsFinished;

        public float DashCooldownProgress => _dashCooldownTimer.Progress;

        public override void OnEnter()
        {
            _dashCooldownTimer.Start();
            _dashTriggerTimer.Start();
            
            animator.CrossFade(DashHash, k_crossFadeDuration, 0);
            
            EventBus<TriggerDash>.Raise(new TriggerDash()
            {
                Character =  character,
            });
            
            TEMP_FXHandler.Instance.InstantiateDashFX(character.transform, character.Stats.GetStrikeRadius() * 0.5f);
            if(debug)
                Logs.Log("Entering Dash State");
            
            Vector3 dashDir = character.Controller.GetDashDirection();
            character.Controller.rigidbody.AddForce(dashDir * 7.2f, ForceMode.Impulse);
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
            _dashTriggerTimer.Stop();
            
            if(debug) 
                Logs.Log("Exiting Dash State");
        }

        public void Reset() {
            _dashCooldownTimer.Reset();
            _dashCooldownTimer.Stop();
            
            _dashTriggerTimer.Reset();
            _dashTriggerTimer.Stop();
        }

        // While dashing, the strike is executed every frame to bump other players or interact with objects.
        private void ExecuteStrike()
        {
            var origin = character.transform.position;
            var count = Physics.OverlapSphereNonAlloc(origin, character.Stats.GetStrikeRadius(), _overlapBuffer);

            // Pour éviter de détecter plusieurs fois les mêmes objets ou joueurs
            var processedRoots = new HashSet<GameObject>();
            var hitCharacters = new HashSet<PlayerCharacter>();
            // var blockedBombshells = new HashSet<Bombshell>();
            
            for (var i = 0; i < count; i++)
            {
                var hit = _overlapBuffer[i];
                if (hit == null) continue;
                 
                var root = hit.transform.root.gameObject;
                
                if (!processedRoots.Add(root)) continue;

                // WAS USED TO DESTROY BOMBSHELLS
                // if (hit.TryGetComponent(out Bombshell bombshell))
                // {
                //     blockedBombshells.Add(bombshell);
                //     bombshell.ReturnToPool();
                //
                //     continue;
                // }
                
                if (hit.TryGetComponent(out IInteractable interactable) && interactable.IsDashInteractable)
                {
                    interactable.Interact();
                    continue;
                }

                var other = hit.GetComponentInParent<PlayerCharacter>();
                if (other == null) continue;
                if (other == character) continue;

                hitCharacters.Add(other);
                
                int dashDamage = Mathf.RoundToInt(character.Stats.StrikeDamage.Value);
                if (dashDamage > 0 && other.Health.IsAlive)
                {
                    other.Health.TakeDamage(dashDamage, character);
                }

                var knockbackDir = (other.transform.position - character.transform.position).normalized;
                var knockbackForce = knockbackDir * character.Stats.GetDashPushForce();
                float knockbackDuration = character.Stats.StrikeKnockbackDuration.Value;
                float stunDuration = character.Stats.GetKnockbackStunDuration();
                other.ReceiveKnockback(knockbackDuration, knockbackForce, stunDuration, character);
            }

            if (hitCharacters.Count > 0)
            {
                EventBus<TriggerStrike>.Raise(new TriggerStrike()
                {
                    Character =  character,
                    HitCharacters = hitCharacters.ToArray(),
                });
            }

            // if (blockedBombshells.Count > 0)
            // {
            //     EventBus<TriggerStrikeHitBombshell>.Raise(new TriggerStrikeHitBombshell()
            //     {
            //         Character = character,
            //         HitBombshells = blockedBombshells.ToArray(),
            //     });
            // }
        }

        private void UpdateDashCooldown() {
            float dashCooldown = character.Stats.GetDashCooldown();
            _dashCooldownTimer.DynamicUpdate(dashCooldown);
        }

        private void UpdateDashDuration() {
            float dashDuration = character.Stats.DashDuration.Value;
            _dashTriggerTimer.DynamicUpdate(dashDuration);
        }

        public override void Dispose() {
            character.Stats.DashCooldown.OnDirtyUpdated -= UpdateDashCooldown;
            character.Stats.DashDuration.OnDirtyUpdated -= UpdateDashDuration;
        }
    }
}