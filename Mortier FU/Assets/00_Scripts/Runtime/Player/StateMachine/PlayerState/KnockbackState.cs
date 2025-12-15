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
        
        public void ReceiveKnockback(float duration, Vector3 bumpForce, float stunDuration)
        {
            // On autorise actuellement le "refresh" du stun
            if(IsActive && _stunTimer.CurrentTime > duration)
                return;
            
            //set bump direction
            _currentBumpForce = bumpForce;
            StunDuration = stunDuration;
            
            _stunTimer.Reset(duration);
            _stunTimer.Start();
        }
        
        public override void OnEnter()
        {
            character.Controller.ResetVelocity();
            
            //Apply Knockback
            character.Controller.ApplyKnockback(_currentBumpForce);
            
            EventBus<TriggerGetStrike>.Raise(new TriggerGetStrike()
            {
                Character = character,
            });
            
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