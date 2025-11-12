using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class LocomotionState : BaseState
    {
        public LocomotionState(PlayerCharacter character, Animator animator) : base(character, animator)
        { }

        public override void OnEnter()
        {
            if(debug) 
                Logs.Log("Entering Locomotion State");
            
            animator.CrossFade(LocomotionHash, k_crossFadeDuration);
        }
        
        public override void Update()
        {
            character.Controller.HandleMovementUpdate();
        }

        public override void FixedUpdate()
        {
            character.Controller.HandleMovementFixedUpdate();
        }
        
        public override void OnExit()
        {
            if(debug)
                Logs.Log("Exiting Locomotion State");
        }
    }
}
