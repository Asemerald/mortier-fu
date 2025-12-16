using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class AimState : BaseState
    {
        public AimState(PlayerCharacter character, Animator animator) : base(character, animator)
        { }

        public override void OnEnter()
        {
            if(debug)
                Logs.Log("Entering Aim State", character.gameObject);
            
            animator.CrossFade(LocomotionHash, k_crossFadeDuration);
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
                Logs.Log("Exiting Aim State");
        }
    }
}