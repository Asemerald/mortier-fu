using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class StunState : BaseState
    {
        private CountdownTimer _stunTimer;

        public StunState(PlayerCharacter character, Animator animator) : base(character, animator)
        {
            _stunTimer = new CountdownTimer(0f);
        }

        private Vector3 currentBumpDir;

        public bool IsActive => _stunTimer.IsRunning;
        
        public override void Update()
        {
            character.Controller.HandleMovementUpdate();
        }
        
        public override void FixedUpdate()
        {
            character.Controller.HandleMovementFixedUpdate();
        }
        
        public void ReceiveStun(float duration, Vector3 bumpDirection)
        {
            // On autorise actuellement le "refresh" du stun
            if(IsActive && _stunTimer.CurrentTime > duration)
                return;
            
            //set bump direction
            currentBumpDir = bumpDirection;
            
            _stunTimer.Reset(duration);
            _stunTimer.Start();
        }
        
        public override void OnEnter()
        {
            character.Controller.ResetVelocity();
            
            //Apply Knockback
            character.Controller.ApplyKnockback(currentBumpDir * 10.5f);
            
            EventBus<TriggerGetStrike>.Raise(new TriggerGetStrike()
            {
                Character = character,
            });
            
            if(debug)
                Logs.Log("Entering Stun State");
        }

        public override void OnExit()
        {
            if(debug) 
                Logs.Log("Exiting Stun State");
        }
    }
}