using MortierFu.Shared;

namespace MortierFu
{
    public class LocomotionState : BaseState
    {
        public LocomotionState(PlayerController playerController) : base(playerController) {}

        public override void OnEnter()
        {
            if(_debug) 
                Logs.Log("Entering Locomotion State");
        }
        
        public override void Update()
        {
            _playerController.HandleMovementUpdate();
        }

        public override void FixedUpdate()
        {
            _playerController.HandleMovementFixedUpdate();
        }
        
        public override void OnExit()
        {
            if(_debug)
                Logs.Log("Exiting Locomotion State");
        }
    }
}
