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

        protected SO_CharacterStats CharacterStats => mortar.Character.CharacterStats;
        
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
        
        public virtual void Update()
        { }
    }

    public static class MortarShootStrategyFactory
    {
        public static MortarShootStrategy Create(ShootMode mode, MortarCharacterComponent mortar, InputAction aimAction, InputAction shootAction)
        {
            return mode switch
            {
                ShootMode.PositionLimited => new MSSPositionLimited(mortar, aimAction, shootAction),
                ShootMode.PositionFree => new MSSPositionFree(mortar, aimAction, shootAction),
                ShootMode.DirectionMaxDistanceOnly => new MSSDirectionMaxDistanceOnly(mortar, aimAction, shootAction),
                ShootMode.DirectionLimited => new MSSDirectionLimited(mortar, aimAction, shootAction),
                ShootMode.Charge => new MSSCharge(mortar, aimAction, shootAction),
                ShootMode.DirectionAutoTarget => new MSSDirectionAutoTarget(mortar, aimAction, shootAction),
                _ => throw new System.ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }
    }
    
    public enum ShootMode
    {
        PositionLimited,
        PositionFree,
        DirectionMaxDistanceOnly,
        DirectionLimited,
        Charge,
        DirectionAutoTarget
    }
}