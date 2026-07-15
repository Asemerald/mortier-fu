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
            character.Controller.ResetVelocity();
            
            animator.CrossFade(DeathHash, k_crossFadeDuration, 0);
            
            character.Owner.DespawnInGame();
        }
        
        public override void OnExit()
        { }
    }
}