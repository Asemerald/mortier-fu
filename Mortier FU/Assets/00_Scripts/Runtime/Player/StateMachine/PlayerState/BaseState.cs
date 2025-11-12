using UnityEngine;

namespace MortierFu
{
    public abstract class BaseState : IState
    {
        protected readonly PlayerCharacter character;
        protected readonly Animator animator;

        protected bool debug = false;
        
        protected static readonly int LocomotionHash = Animator.StringToHash("Locomotion");
        protected static readonly int ShootHash = Animator.StringToHash("Shoot");
        
        protected const float k_crossFadeDuration = 0.1f; 
        
        protected BaseState(PlayerCharacter character, Animator animator)
        {
            this.character = character;
            this.animator = animator;
        }
        
        public virtual void OnEnter() {}

        public virtual void Update() {}

        public virtual void FixedUpdate() {}

        public virtual void OnExit() {}
    }    
}