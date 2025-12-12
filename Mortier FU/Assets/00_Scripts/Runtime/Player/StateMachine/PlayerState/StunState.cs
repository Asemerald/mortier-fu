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