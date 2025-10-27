using MortierFu.Shared;

namespace MortierFu
{
    public class AimState : BaseState
    {
        public AimState(PlayerController playerController) : base(playerController) {}
        
        public override void OnEnter()
        {
            if(_debug)
                Logs.Log("Entering Aim State");
        }
        
        public override void OnExit()
        {
            if(_debug)
                Logs.Log("Exiting Aim State");
        }
    }
}