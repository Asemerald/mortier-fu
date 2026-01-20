using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class KnockbackState : BaseState
    {
        private CountdownTimer _knockbackTimer;

        public KnockbackState(PlayerCharacter character, Animator animator) : base(character, animator)
        {
            _knockbackTimer = new CountdownTimer(0f);
        }

        private object _lastBumpSource;
        private PlayerCharacter _lastPusher;
        private float _lastBumpTime;
        private Vector3 _currentBumpForce;
        
        public float StunDuration { get; private set; }

        public bool IsActive => _knockbackTimer.IsRunning;
        
        public object LastBumpSource => _lastBumpSource;
        public PlayerCharacter LastPusher => _lastPusher;

        public float ComputeLastBumpElapsedTime() => Time.time - _lastBumpTime;
        
        public override void Update()
        {
            character.Controller.HandleMovementUpdate();
        }
        
        public override void FixedUpdate()
        {
            character.Controller.HandleMovementFixedUpdate();
        }
        
        public void ReceiveKnockback(float duration, Vector3 bumpForce, float stunDuration, object source)
        {
            // Prevent getting bump by the same source multiple times
            if (IsActive && _lastBumpSource == source)
                return;
            
            // On autorise actuellement le "refresh" du stun
            // if(IsActive && _stunTimer.CurrentTime > duration)
            //     return;
            
            //set bump direction
            _currentBumpForce = bumpForce;
            _lastBumpSource = source;
            
            // Pushed by a player
            if (source is PlayerCharacter pusher) {
                _lastPusher = pusher;
            } else if(ComputeLastBumpElapsedTime() > 8f) {
                _lastPusher = null;
            }
            
            _lastBumpTime = Time.time;
            
            StunDuration = stunDuration;
            
            _knockbackTimer.Reset(duration);
            _knockbackTimer.Start();
            
            //character.Controller.ResetVelocity();
            
            //Apply Knockback
            character.Controller.ApplyKnockback(_currentBumpForce);
            
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Strike_Knockback, character.transform.position);
            character.ShakeService.ShakeController(character.Owner, ShakeService.ShakeType.MID);
            
            EventBus<TriggerBumpedByPlayer>.Raise(new TriggerBumpedByPlayer()
            {
                Character = character,
            });
        }
        
        public override void OnEnter()
        {
            if(debug)
                Logs.Log("Entering Knockback State");
        }

        public override void OnExit()
        {
            _knockbackTimer.Stop();
            
            if(debug) 
                Logs.Log("Exiting Knockback State");
        }

        public void Reset() {
            _lastBumpSource = null;
            _lastPusher = null;
        }

        public void ClearLastBumpSource() => _lastBumpSource = null;
    }
}