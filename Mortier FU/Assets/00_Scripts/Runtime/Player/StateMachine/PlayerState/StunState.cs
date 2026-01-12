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

        public bool IsActive => _stunTimer.IsRunning;
        
        public void ReceiveStun(float duration)
        {
            // On autorise actuellement le "refresh" du stun
            if(IsActive && _stunTimer.CurrentTime > duration)
                return;
            
            SystemManager.Instance.Get<CameraSystem>().Controller.Shake(1, 5, 1);
            
            _stunTimer.Reset(duration);
            _stunTimer.Start();
        }
        
        public override void OnEnter()
        {
            character.Controller.ResetVelocity();
            character.Controller.rigidbody.isKinematic = true;
            
            EventBus<TriggerBumpedByPlayer>.Raise(new TriggerBumpedByPlayer()
            {
                Character = character,
            });
            
            if(debug)
                Logs.Log("Entering Stun State");
        }

        public override void OnExit()
        {            
            character.Controller.rigidbody.isKinematic = false;

            if(debug) 
                Logs.Log("Exiting Stun State");
        }
    }
}