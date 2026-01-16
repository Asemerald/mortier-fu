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
            
            character.Controller.ResetVelocity();
            SystemManager.Instance.Get<CameraSystem>().Controller.Shake(1, 10, 1);
            
            _stunTimer.Reset(duration);
            _stunTimer.Start();
        }
        
        public override void OnEnter()
        {
            character.Controller.ResetVelocity();

            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Player_Stun, character.transform.position);
            ShakeService.ShakeController(character.Owner, ShakeService.ShakeType.MID);
            
            EventBus<TriggerBumpedByPlayer>.Raise(new TriggerBumpedByPlayer()
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