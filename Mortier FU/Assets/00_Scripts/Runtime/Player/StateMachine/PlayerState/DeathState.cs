using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class DeathState : BaseState
    {
        public DeathState(PlayerCharacter character, Animator animator) : base(character, animator)
        { }

        public override void OnEnter()
        {
            if(debug) Logs.Log("Entering Death State");
            
            character.Controller.ResetVelocity();
            character.Owner.DespawnInGame();
        }
        
        public override void OnExit()
        {
            if (debug) Logs.Log("Exiting Death State");
        }
    }
}