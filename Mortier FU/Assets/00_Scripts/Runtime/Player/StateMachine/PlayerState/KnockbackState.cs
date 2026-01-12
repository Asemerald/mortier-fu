using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class KnockbackState : BaseState
    {
        private CountdownTimer _stunTimer;

        public KnockbackState(PlayerCharacter character, Animator animator) : base(character, animator)
        {
            _stunTimer = new CountdownTimer(0f);
        }

        private object _lastBumpSource;
        private Vector3 _currentBumpForce;
        
        public float StunDuration { get; private set; }

        public bool IsActive => _stunTimer.IsRunning;
        
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
            
            StunDuration = stunDuration;
            
            _stunTimer.Reset(duration);
            _stunTimer.Start();
            
            character.Controller.ResetVelocity();
            
            //Apply Knockback
            character.Controller.ApplyKnockback(_currentBumpForce);
            
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
            if(debug) 
                Logs.Log("Exiting Knockback State");
        }
    }
}