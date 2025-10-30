using MortierFu.Shared;

namespace MortierFu
{
    public class StrikeState : BaseState
    {
        public StrikeState(PlayerController playerController) : base(playerController) {}
        
        public override void OnEnter()
        {
            _playerController.EnterStrikeState();
            
            //Call FX 
            TEMP_FXHandler.Instance.InstantiateStrikeFX(_playerController.transform, _playerController.CharacterStats.StrikeRadius.Value);

            
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