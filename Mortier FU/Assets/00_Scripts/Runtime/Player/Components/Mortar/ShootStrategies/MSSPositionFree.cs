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
            aimWidget.AttachedToTarget = false;
            aimWidget.Target = null;
            aimWidget.SetRelativePosition(Vector3.zero);
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
            var aimInput = aimAction.ReadValue<Vector2>();
            
            if (aimInput.sqrMagnitude < k_minAimInputLength)
                return;
            
            Vector3 newPos = mortar.AimWidget.RelativePosition + new Vector3(aimInput.x, 0.0f, aimInput.y) * (Time.deltaTime * mortar.AimWidgetSpeed);
            mortar.AimWidget.SetRelativePosition(newPos);
        }

        private void OnShoot(InputAction.CallbackContext ctx)
        {
            mortar.Shoot();
        }
    }
}