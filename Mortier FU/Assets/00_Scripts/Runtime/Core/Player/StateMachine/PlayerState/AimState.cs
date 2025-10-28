using MortierFu.Shared;

namespace MortierFu
{
    public class AimState : BaseState
    {
        public AimState(PlayerController playerController) : base(playerController) {}
        
        public override void OnEnter()
        {
            // Se bind a l'input de tir
            if(_debug)
                Logs.Log("Entering Aim State");
        }
        
        public override void OnExit()
        {
            // Se debind de l'input de tir
            if(_debug)
                Logs.Log("Exiting Aim State");
        }
    }
}