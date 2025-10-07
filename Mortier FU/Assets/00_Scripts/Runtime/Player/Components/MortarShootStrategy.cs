using UnityEngine;

namespace MortierFu
{
    public abstract class MortarShootStrategy
    {
        protected readonly Mortar mortar;
        
        protected MortarShootStrategy(Mortar mortar)
        {
            this.mortar = mortar;
        } 
        
        public virtual void Initialize()
        { }
        
        public virtual void DeInitialize()
        { }
        
        public virtual void BeginAiming()
        { }

        public virtual void UpdateAiming(Vector2 aimInput)
        { }
        
        public virtual void EndAiming()
        { }

        public virtual bool CanShoot() => true;
    }

    public class MortarShootStrategy_PositionLimited : MortarShootStrategy
    {
        public MortarShootStrategy_PositionLimited(Mortar mortar) : base(mortar)
        { }
        
        public override void Initialize()
        {
            var aimWidget = mortar.AimWidget;
            
            aimWidget.IsActive = true;
            aimWidget.AttachedToTarget = true;
            aimWidget.Target = mortar.transform;
            aimWidget.RelativePosition = Vector3.zero;
            aimWidget.Show();
        }

        public override void UpdateAiming(Vector2 aimInput)
        {
            var aimWidget = mortar.AimWidget;
            aimWidget.RelativePosition += new Vector3(aimInput.x, 0.0f, aimInput.y) * (Time.deltaTime * mortar.AimWidgetSpeed);
            aimWidget.RelativePosition = Vector3.ClampMagnitude(aimWidget.RelativePosition, mortar.ShotRange.Value);
        }
    }
}