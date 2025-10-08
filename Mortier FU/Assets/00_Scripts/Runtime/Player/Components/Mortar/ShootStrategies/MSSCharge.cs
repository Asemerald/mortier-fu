using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    // Either relative or absolute: meaning the charge takes longer to reach max if the mortar range is higher (RELATIVE)
    public class MSSCharge : MortarShootStrategy
    {
        private float _chargeSpeed;
        
        private bool _isCharging;
        private float _currentCharge;
        private Vector2 _currentAimInput;

        public MSSCharge(Mortar mortar, InputAction aimAction, InputAction shootAction, float chargeSpeed = 1.0f) :
            base(mortar, aimAction, shootAction)
        {
            _chargeSpeed = chargeSpeed;
            _currentCharge = 0.0f;
        }
        
        public void SetChargeSpeed(float speed) => _chargeSpeed = speed;
        
        public override void Initialize()
        {
            var aimWidget = mortar.AimWidget;

            aimWidget.IsActive = false;
            aimWidget.AttachedToTarget = true;
            aimWidget.Target = mortar.transform;
            aimWidget.SetRelativePosition(Vector3.zero);
            aimWidget.Hide();
            
            // Bind to actions
            aimAction.performed += OnAiming;
            shootAction.performed += BeginCharging;
            shootAction.canceled += EndCharging;
        }

        public override void DeInitialize()
        {
            // Unbind from actions
            aimAction.performed -= OnAiming;
            shootAction.performed -= BeginCharging;
            shootAction.canceled -= EndCharging;
        }

        public override void Update()
        {
            if (!_isCharging) return;
            
            _currentCharge += Time.deltaTime * _chargeSpeed;
            _currentCharge = Mathf.Clamp01(_currentCharge); 
            
            Vector3 newPos = new Vector3(_currentAimInput.x, 0.0f, _currentAimInput.y) * (mortar.ShotRange.Value * _currentCharge);
            mortar.AimWidget.SetRelativePosition(newPos);
        }
        
        private void BeginCharging(InputAction.CallbackContext ctx)
        {
            if (!mortar.CanShoot || _isCharging) return;
            
            _currentCharge = 0.0f;
            _isCharging = true;

            var aimWidget = mortar.AimWidget;
            aimWidget.IsActive = true;
            aimWidget.SetRelativePosition(Vector3.zero);;
            aimWidget.Show();
        }
        
        private void EndCharging(InputAction.CallbackContext ctx)
        {
            if (!_isCharging)
                return;
            
            _isCharging = false;
            mortar.AimWidget.IsActive = false;
            mortar.AimWidget.Hide();
            
            mortar.Shoot();
        }
        
        private void OnAiming(InputAction.CallbackContext ctx)
        {
            var aimInput = ctx.ReadValue<Vector2>();

            if (aimInput.sqrMagnitude < k_minAimInputLength)
                return;

            _currentAimInput = aimInput.normalized;
        }
    }
}