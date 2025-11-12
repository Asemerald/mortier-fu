using MortierFu.Shared;

namespace MortierFu
{
    public class LocomotionState : BaseState
    {
        public LocomotionState(PlayerCharacter character) : base(character) {}

        public override void OnEnter()
        {
            if(debug) 
                Logs.Log("Entering Locomotion State");
        }
        
        public override void Update()
        {
            character.Controller.HandleMovementUpdate();
        }

        public override void FixedUpdate()
        {
            character.Controller.HandleMovementFixedUpdate();
        }
        
        public override void OnExit()
        {
            if(debug)
                Logs.Log("Exiting Locomotion State");
        }
    }
}
