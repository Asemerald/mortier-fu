using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public class MSSDirectionLimited : MortarShootStrategy
    {
        public MSSDirectionLimited(Mortar mortar, InputAction aimAction, InputAction shootAction) : base(mortar, aimAction, shootAction)
        { }


        public override void Initialize()
        {
            var aimWidget = mortar.AimWidget;
            
            aimWidget.IsActive = true;
            aimWidget.Origin = Vector3.zero;
            aimWidget.RelativePosition = mortar.transform.forward * mortar.ShotRange.Value;
            aimWidget.AttachedToTarget = true;
            aimWidget.Target = mortar.transform;
            aimWidget.Show();
            
            // Bind input actions
            shootAction.performed += OnShoot;
        }

        public override void DeInitialize()
        {
            // Unbind input actions
            shootAction.performed -= OnShoot;
        }

        public override void Update()
        {
            Vector2 aimInput = aimAction.ReadValue<Vector2>();
            
            
            // Use the analog of the stick to know how far the shell should go
            float inputStrength = Mathf.Clamp01(aimInput.magnitude);
            float strength = mortar.ShotRange.Value * inputStrength;
            var aimWidget = mortar.AimWidget;
            
            aimWidget.RelativePosition = new Vector3(aimInput.x, 0.0f, aimInput.y).normalized * strength;
        }
        
        private void OnShoot(InputAction.CallbackContext ctx)
        {
            mortar.Shoot();
        }
    }
}