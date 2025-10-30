using MortierFu.Shared;

namespace MortierFu
{
    public class StunState : BaseState
    {
        public StunState(PlayerCharacter character) : base(character) {}
        
        public override void OnEnter()
        {
            character.Controller.EnterStunState();
            
            if(debug)
                Logs.Log("Entering Stun State");
        }

        public override void OnExit()
        {
            character.Controller.ExitStunState();
            
            if(debug) 
                Logs.Log("Exiting Stun State");
        }
    }
}