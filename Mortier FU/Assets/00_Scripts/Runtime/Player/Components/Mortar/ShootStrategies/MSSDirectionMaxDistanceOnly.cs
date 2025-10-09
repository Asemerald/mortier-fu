using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public class MSSDirectionMaxDistanceOnly : MortarShootStrategy
    {
        public MSSDirectionMaxDistanceOnly(Mortar mortar, InputAction aimAction, InputAction shootAction) : base(mortar, aimAction, shootAction)
        { }

        public override void Initialize()
        {
            aimWidget.IsActive = true;
            aimWidget.Origin = Vector3.zero;
            aimWidget.AttachedToTarget = true;
            aimWidget.Target = mortar.transform;
            aimWidget.SetRelativePosition(mortar.transform.forward * characterData.ShotRange.Value);
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
            var aimInput = ctx.ReadValue<Vector2>();
            
            if (aimInput.sqrMagnitude < k_aimDeadZone)
                return;
            
            var newPos = new Vector3(aimInput.x, 0.0f, aimInput.y).normalized * characterData.ShotRange.Value;
            aimWidget.SetRelativePosition(newPos);
        }
        
        private void OnShoot(InputAction.CallbackContext ctx)
        {
            mortar.Shoot();
        }
    }
}