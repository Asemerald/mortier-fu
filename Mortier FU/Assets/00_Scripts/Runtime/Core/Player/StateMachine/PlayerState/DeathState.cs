using MortierFu.Shared;

namespace MortierFu
{
    public class DeathState : BaseState
    {
        public DeathState(PlayerController playerController) : base(playerController) {}
        
        public override void OnEnter()
        {
            if(_debug) 
                Logs.Log("Entering Death State");
            
            _playerController.HandleDeath();
        }
        
        public override void OnExit()
        {
            if(_debug)
                Logs.Log("Exiting Death State");
        }
    }
}