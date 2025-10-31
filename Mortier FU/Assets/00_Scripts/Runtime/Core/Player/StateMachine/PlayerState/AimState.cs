using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class AimState : BaseState
    {
        public AimState(PlayerCharacter character) : base(character) {}
        
        public override void OnEnter()
        {
            character.Mortar.BeginAiming();
            if(debug)
                Logs.Log("Entering Aim State", character.gameObject);
        }

        public override void Update()
        {
            character.Mortar.HandleAimMovement();
        }

        public override void FixedUpdate()
        {
            character.Controller.HandleMovement();
            character.Controller.HandleRotation();
        }

        public override void OnExit()
        {
            character.Mortar.EndAiming();
            if(debug)
                Logs.Log("Exiting Aim State");
        }
    }
}