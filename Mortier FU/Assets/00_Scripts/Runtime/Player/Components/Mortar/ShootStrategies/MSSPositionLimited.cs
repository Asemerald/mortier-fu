using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public class MSSPositionLimited : MortarShootStrategy
    {
        public MSSPositionLimited(Mortar mortar, InputAction aimAction, InputAction shootAction) : base(mortar, aimAction, shootAction)
        { }
        
        public override void Initialize()
        {
            var aimWidget = mortar.AimWidget;
            
            aimWidget.IsActive = true;
            aimWidget.AttachedToTarget = true;
            aimWidget.Target = mortar.transform;
            aimWidget.RelativePosition = mortar.transform.forward * (mortar.ShotRange.Value * 0.5f);
            aimWidget.Show();
            
            // Bind to actions
            shootAction.performed += OnShoot;
        }

        public override void DeInitialize()
        {
            // Unbind from actions
            shootAction.performed -= OnShoot;
        }

        public override void Update()
        {
            var aimWidget = mortar.AimWidget;
            Vector2 aimInput = aimAction.ReadValue<Vector2>();

            if (aimInput.sqrMagnitude < k_minAimInputLength)
                return;
            
            aimWidget.RelativePosition += new Vector3(aimInput.x, 0.0f, aimInput.y) * (Time.deltaTime * mortar.AimWidgetSpeed);
            aimWidget.RelativePosition = Vector3.ClampMagnitude(aimWidget.RelativePosition, mortar.ShotRange.Value);
        }
        
        private void OnShoot(InputAction.CallbackContext ctx)
        {
            mortar.Shoot();
        }
    }
}