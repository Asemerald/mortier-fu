using MortierFu.Shared;

namespace MortierFu
{
    public class HitState : BaseState
    {
        public HitState(PlayerController playerController) : base(playerController) {}
        
        public override void OnEnter()
        {
            _playerController.EnterHitState();
            
            if(_debug)
                Logs.Log("Entering Hit State");
        }

        public override void Update()
        {
            _playerController.ExecuteStun();
            _playerController.HandleMovementUpdate();
        }

        public override void FixedUpdate()
        {
            _playerController.HandleMovementFixedUpdate();
        }

        public override void OnExit()
        {
            _playerController.ExitHitState();
            
            if(_debug) 
                Logs.Log("Exiting Hit State");
        }
    }
}