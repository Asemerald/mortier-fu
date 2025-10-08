using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public class MSSPositionFree : MortarShootStrategy
    {
        public MSSPositionFree(Mortar mortar, InputAction aimAction, InputAction shootAction) : base(mortar, aimAction, shootAction)
        { }

        public override void Initialize()
        {
            var aimWidget = mortar.AimWidget;
            
            aimWidget.IsActive = true;
            aimWidget.Origin = Vector3.zero;
            aimWidget.RelativePosition = mortar.transform.forward * (mortar.ShotRange.Value * 0.5f);
            aimWidget.AttachedToTarget = false;
            aimWidget.Target = null;
            aimWidget.Show();
            
            // Bind input actions
            aimAction.performed += OnAiming;
            shootAction.performed += OnShoot;
        }
        
        public override void DeInitialize()
        {
            // Unbind input actions
            aimAction.performed -= OnAiming;
            shootAction.performed -= OnShoot;
        }

        private void OnAiming(InputAction.CallbackContext ctx)
        {
            var aimWidget = mortar.AimWidget;
            var aimInput = ctx.ReadValue<Vector2>();
            
            if (aimInput.sqrMagnitude < k_minAimInputLength)
                return;
            
            aimWidget.RelativePosition += new Vector3(aimInput.x, 0.0f, aimInput.y) * (Time.deltaTime * mortar.AimWidgetSpeed);
        }

        private void OnShoot(InputAction.CallbackContext ctx)
        {
            mortar.Shoot();
        }
    }
}