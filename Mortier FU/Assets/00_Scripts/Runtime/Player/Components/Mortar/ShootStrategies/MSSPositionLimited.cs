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
            aimWidget.IsActive = true;
            aimWidget.AttachedToTarget = true;
            aimWidget.Target = mortar.transform;
            aimWidget.SetRelativePosition(mortar.transform.forward * (CharacterStats.ShotRange.Value * 0.5f));
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
            Vector2 aimInput = aimAction.ReadValue<Vector2>();

            if (aimInput.sqrMagnitude < k_minAimInputLength)
                return;
            
            Vector3 offset = new Vector3(aimInput.x, 0.0f, aimInput.y) * (Time.deltaTime * CharacterStats.AimWidgetSpeed.Value);
            Vector3 newPos = aimWidget.RelativePosition + offset;
            newPos = Vector3.ClampMagnitude(newPos, CharacterStats.ShotRange.Value);
            aimWidget.SetRelativePosition(newPos);
        }
        
        private void OnShoot(InputAction.CallbackContext ctx)
        {
            mortar.Shoot();
        }
    }
}