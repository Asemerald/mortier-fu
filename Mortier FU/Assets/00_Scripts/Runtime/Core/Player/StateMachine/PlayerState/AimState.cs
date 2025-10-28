using MortierFu.Shared;

namespace MortierFu
{
    public class AimState : BaseState
    {
        public AimState(PlayerController playerController) : base(playerController) {}
        
        public override void OnEnter()
        {
            _playerController.Mortar.BeginAiming();
            if(_debug)
                Logs.Log("Entering Aim State", _playerController.gameObject);
        }

        public override void Update()
        {
            _playerController.Mortar.HandleAimMovement();
            _playerController.HandleMovementUpdate();
        }

        public override void FixedUpdate()
        {
            _playerController.HandleMovementFixedUpdate();
        }

        public override void OnExit()
        {
            _playerController.Mortar.EndAiming();
            if(_debug)
                Logs.Log("Exiting Aim State");
        }
    }
}