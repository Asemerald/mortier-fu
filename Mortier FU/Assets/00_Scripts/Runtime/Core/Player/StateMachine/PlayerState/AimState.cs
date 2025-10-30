using MortierFu.Shared;

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
            character.Controller.HandleMovementUpdate(0.7f);
        }

        public override void FixedUpdate()
        {
            character.Controller.HandleMovementFixedUpdate();
        }

        public override void OnExit()
        {
            character.Mortar.EndAiming();
            if(debug)
                Logs.Log("Exiting Aim State");
        }
    }
}