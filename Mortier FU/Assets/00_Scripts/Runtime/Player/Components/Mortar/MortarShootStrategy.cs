using UnityEngine.InputSystem;

namespace MortierFu
{
    public abstract class MortarShootStrategy // MSS
    {
        protected readonly Mortar mortar;
        protected readonly InputAction aimAction;
        protected readonly InputAction shootAction;

        protected const float k_minAimInputLength = 0.001f;
        
        protected MortarShootStrategy(Mortar mortar, InputAction aimAction, InputAction shootAction)
        {
            this.mortar = mortar;
            this.aimAction = aimAction;
            this.shootAction = shootAction;
        }
        
        public virtual void Initialize()
        { }
        
        public virtual void DeInitialize()
        { }
        
        public virtual void Update()
        { }
    }
}