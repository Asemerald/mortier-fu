using UnityEngine.InputSystem;

namespace MortierFu
{
    public abstract class MortarShootStrategy // MSS
    {
        protected readonly MortarCharacterComponent mortar;
        protected readonly AimWidget aimWidget;
        protected readonly InputAction aimAction;
        protected readonly InputAction shootAction;

        protected const float k_minAimInputLength = 0.0001f;
        protected const float k_aimDeadZone = 0.2f;

        protected SO_CharacterStats CharacterStats => mortar.Character.Stats;
        
        protected MortarShootStrategy(MortarCharacterComponent mortar, InputAction aimAction, InputAction shootAction)
        {
            this.mortar = mortar;
            this.aimWidget = mortar.AimWidget;
            this.aimAction = aimAction;
            this.shootAction = shootAction;
        }
        
        public virtual void Initialize()
        { }
        
        public virtual void DeInitialize()
        { }
        
        public virtual void BeginAiming()
        { }
        
        public virtual void EndAiming()
        { }
        
        public virtual void CancelAiming()
        { }
        
        public virtual void Update()
        { }
    }
}