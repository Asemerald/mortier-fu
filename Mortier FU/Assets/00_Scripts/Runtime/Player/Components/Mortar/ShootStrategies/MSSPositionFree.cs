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
            aimWidget.IsActive = true;
            aimWidget.Origin = Vector3.up * 0.1f;
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

            Vector3 offset = new Vector3(aimInput.x, 0.0f, aimInput.y) * (Time.deltaTime * characterData.AimWidgetSpeed.Value);
            Vector3 newPos = mortar.AimWidget.RelativePosition + offset;
            mortar.AimWidget.SetRelativePosition(newPos);
        }

        private void OnShoot(InputAction.CallbackContext ctx)
        {
            mortar.Shoot();
        }
    }
}