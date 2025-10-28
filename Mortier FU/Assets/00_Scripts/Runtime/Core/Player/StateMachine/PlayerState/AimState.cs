using MortierFu.Shared;

namespace MortierFu
{
    public class AimState : BaseState
    {
        public AimState(PlayerController playerController) : base(playerController) {}
        
        public override void OnEnter()
        {
            _playerController.Mortar.EnableShoot();
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
            _playerController.Mortar.DisableShoot();
            if(_debug)
                Logs.Log("Exiting Aim State");
        }
    }
}