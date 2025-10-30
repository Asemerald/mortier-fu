using MortierFu.Shared;

namespace MortierFu
{
    public class StrikeState : BaseState
    {
        public StrikeState(PlayerCharacter character) : base(character) {}
        
        public override void OnEnter()
        {
            character.Controller.EnterStrikeState();
            
            if(debug)
                Logs.Log("Entering Strike State");
        }

        public override void Update()
        {
            character.Controller.ExecuteStrike();
            character.Controller.HandleMovementUpdate(0.2f);
        }

        public override void FixedUpdate()
        {
            character.Controller.HandleMovementFixedUpdate();
        }

        public override void OnExit()
        {
            character.Controller.ExitStrikeState();
            
            if(debug) 
                Logs.Log("Exiting Strike State");
        }
    }
}