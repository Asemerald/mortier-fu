using MortierFu.Shared;

namespace MortierFu
{
    public class StunState : BaseState
    {
        public StunState(PlayerController playerController) : base(playerController) {}
        
        public override void OnEnter()
        {
            if(_debug)
                Logs.Log("Entering Stun State");
        }

        public override void Update()
        {
            _playerController.HandleStun();
        }

        public override void OnExit()
        {
            if(_debug) 
                Logs.Log("Exiting Stun State");
        }
    }
}