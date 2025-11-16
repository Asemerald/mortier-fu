using System.Collections.Generic;
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
            
            TEMP_FXHandler.Instance.InstantiateStrikeFX(character.transform, character.Stats.StrikeRadius.Value);
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
            _strikeCooldownTimer.Stop();
            _strikeTriggerTimer.Stop();
        }
        
        private void ExecuteStrike()
        {
            var origin = character.transform.position;
            var count = Physics.OverlapSphereNonAlloc(origin, character.Stats.StrikeRadius.Value, _overlapBuffer);

            // Pour éviter de détecter plusieurs fois les mêmes objets ou joueurs
            var processedRoots = new HashSet<GameObject>();
            
            for (var i = 0; i < count; i++)
            {
                var hit = _overlapBuffer[i];
                if (hit == null) continue;
                 
                var root = hit.transform.root.gameObject;
                 
                if (!processedRoots.Add(root)) continue;

                if (hit.TryGetComponent(out Bombshell bombshell))
                {
                    bombshell.ReturnToPool();
                    continue;
                }
                
                // TEMP ajout breakable
                if (hit.TryGetComponent(out Breakable breakable))
                {
                    breakable.DestroyObject(1);
                    continue;
                }
                // TEMP ajout Moveable
                if (hit.TryGetComponent(out Movable movable))
                {
                    movable.InteratableMove();
                    continue;
                }

                var other = hit.GetComponentInParent<PlayerCharacter>();
                if (other == null) continue;
                if (other == character) continue;

                int strikeDamage = Mathf.RoundToInt(character.Stats.StrikeDamage.Value);
                other.Health.TakeDamage(strikeDamage, character);
                other.ReceiveStun(character.Stats.StrikeStunDuration.Value);
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