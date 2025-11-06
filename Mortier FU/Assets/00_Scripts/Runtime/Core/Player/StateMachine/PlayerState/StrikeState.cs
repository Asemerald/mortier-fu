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

        public bool InCooldown => _strikeCooldownTimer.IsRunning;
        public bool IsFinished => _strikeTriggerTimer.IsFinished;
        
        public float StrikeCooldownProgress => _strikeCooldownTimer.Progress;
        
        public StrikeState(PlayerCharacter character) : base(character)
        {
            _strikeCooldownTimer = new CountdownTimer(0f);
            _strikeTriggerTimer = new CountdownTimer(0f);
        }
        
        public override void OnEnter()
        {
            _strikeCooldownTimer.Reset(character.CharacterStats.StrikeCooldown.Value);
            _strikeTriggerTimer.Reset(character.CharacterStats.StrikeDuration.Value);
            
            _strikeCooldownTimer.Start();
            _strikeTriggerTimer.Start();
            
            TEMP_FXHandler.Instance.InstantiateStrikeFX(character.transform, character.CharacterStats.StrikeRadius.Value);
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
        
        private void ExecuteStrike()
        {
            var origin = character.transform.position;
            var count = Physics.OverlapSphereNonAlloc(origin, character.CharacterStats.StrikeRadius.Value, _overlapBuffer);

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

                int strikeDamage = Mathf.RoundToInt(character.CharacterStats.StrikeDamage.Value);
                other.Health.TakeDamage(strikeDamage, character);
                other.ReceiveStun(character.CharacterStats.StrikeStunDuration.Value);
            }
        }
    }
}