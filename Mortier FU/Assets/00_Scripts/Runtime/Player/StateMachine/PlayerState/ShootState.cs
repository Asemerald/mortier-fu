using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class ShootState : BaseState
    {
        public bool IsClipFinished
        {
            get
            {
                if (animator.IsInTransition(0))
                    return false;
                
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                return stateInfo.normalizedTime >= 1f - k_crossFadeDuration;
            }
        }
        
        public ShootState(PlayerCharacter character, Animator animator) : base(character, animator)
        { }
        
        public override void OnEnter()
        {
            if(debug) 
                Logs.Log("Entering Shoot State");

            animator.CrossFade(ShootHash, k_crossFadeDuration, 0);
        }
        
        public override void Update()
        {
            character.Mortar.HandleAimMovement();
            character.Controller.HandleMovementUpdate(0.65f);
        }
        
        public override void FixedUpdate()
        {
            character.Controller.HandleMovementFixedUpdate();
        }
        
        public override void OnExit()
        {
            if(debug)
                Logs.Log("Exiting Shoot State");
            
            character.Mortar.StopShooting();
        }
    }
}