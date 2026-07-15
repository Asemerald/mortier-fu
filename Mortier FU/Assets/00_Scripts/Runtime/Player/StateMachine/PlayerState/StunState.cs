using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class StunState : BaseState
    {
        private CountdownTimer _stunTimer;
        private FXService _fxService;

        public StunState(PlayerCharacter character, Animator animator) : base(character, animator)
        {
            _fxService = ServiceManager.Instance.Get<FXService>();
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
            animator.CrossFade(StunHash, k_crossFadeDuration, 0);
            
            character.Controller.ResetVelocity();

            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Player_Stun, character.transform.position);
            character.ShakeService.ShakeController(character.Owner, ShakeService.ShakeType.MID);
            
            _fxService.PlayStunFX(character.gameObject);
            
            EventBus<TriggerBumpedByPlayer>.Raise(new TriggerBumpedByPlayer()
            {
                Character = character,
            });
        }
        
        public override void FixedUpdate()
        {
            character.Controller.ResetVelocity();
        }

        public override void OnExit()
        {
            _stunTimer.Stop();
        }
    }
}