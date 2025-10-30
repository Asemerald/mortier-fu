using MortierFu.Shared;

namespace MortierFu
{
    public class DeathState : BaseState
    {
        public DeathState(PlayerCharacter character) : base(character) {}
        
        public override void OnEnter()
        {
            if(debug) Logs.Log("Entering Death State");
            
            character.Controller.ResetVelocity();
            character.gameObject.SetActive(false);
        }
        
        public override void OnExit()
        {
            if (debug) Logs.Log("Exiting Death State");
        }
    }
}