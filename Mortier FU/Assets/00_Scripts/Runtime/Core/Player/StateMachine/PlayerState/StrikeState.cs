using MortierFu.Shared;

namespace MortierFu
{
    public class StrikeState : BaseState
    {
        public StrikeState(PlayerController playerController) : base(playerController) {}
        
        public override void OnEnter()
        {
            _playerController.EnterStrikeState();
            
            if(_debug)
                Logs.Log("Entering Strike State");
        }

        public override void Update()
        {
            _playerController.ExecuteStrike();
            _playerController.HandleMovementUpdate(0.2f);
        }

        public override void FixedUpdate()
        {
            _playerController.HandleMovementFixedUpdate();
        }

        public override void OnExit()
        {
            _playerController.ExitStrikeState();
            
            if(_debug) 
                Logs.Log("Exiting Strike State");
        }
    }
}