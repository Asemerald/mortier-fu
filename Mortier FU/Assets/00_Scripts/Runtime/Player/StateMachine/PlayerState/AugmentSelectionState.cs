    using MortierFu.Shared;
    using UnityEngine;
    
namespace MortierFu
{
    public class AugmentSelectionState : BaseState
    {
        public AugmentSelectionState(PlayerCharacter character, Animator animator) : base(character, animator) 
        { }

        public override void OnEnter()
        {
            if(debug)
                Logs.Log("Entering AugmentSelectionState");
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
                Logs.Log("Exiting AugmentSelectionState");
        }
    }
}