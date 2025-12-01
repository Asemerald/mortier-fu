using System.Numerics;
using UnityEngine;
using UnityEngine.InputSystem;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace MortierFu
{
    public class MSSPositionLimited : MortarShootStrategy
    {
        public MSSPositionLimited(MortarCharacterComponent mortar, InputAction aimAction, InputAction shootAction) : base(mortar, aimAction, shootAction)
        { }

        private bool _enableShoot;
        
        public override void Initialize()
        {
            aimWidget.IsActive = true;
            aimWidget.AttachedToTarget = true;
            aimWidget.Target = mortar.Character.transform;
            aimWidget.SetRelativePosition(mortar.Character.transform.forward * (CharacterStats.ShotRange.Value * 0.5f));
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
            //call shot if bShotEnabled
            if (_enableShoot)
            {
                mortar.Shoot();
            }
            
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
            if (ctx.action.WasPressedThisFrame())
            {
                _enableShoot = true;
            }
            
            if(ctx.action.WasReleasedThisFrame())
            {
                _enableShoot = false;
            }
            
        }

        public override void BeginAiming()
        {
            _enableShoot = false;
            
            Vector2 aimInput = aimAction.ReadValue<Vector2>();
            Vector3 newPos = Vector3.ClampMagnitude(new Vector3(aimInput.x, 0.0f, aimInput.y) * CharacterStats.ShotRange.Value, CharacterStats.ShotRange.Value);
            aimWidget.SetRelativePosition(newPos);
        }
    }
}