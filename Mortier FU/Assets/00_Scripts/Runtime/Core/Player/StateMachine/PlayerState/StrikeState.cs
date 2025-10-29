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
                Logs.Log("Entering Hit State");
        }

        public override void Update()
        {
            _playerController.ExecuteStrike();
            _playerController.HandleMovementUpdate();
        }

        public override void FixedUpdate()
        {
            _playerController.HandleMovementFixedUpdate();
        }

        public override void OnExit()
        {
            _playerController.ExitStrikeState();
            
            if(_debug) 
                Logs.Log("Exiting Hit State");
        }
    }
}