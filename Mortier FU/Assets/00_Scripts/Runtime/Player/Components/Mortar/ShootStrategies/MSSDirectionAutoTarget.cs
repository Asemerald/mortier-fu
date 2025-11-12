using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public class MSSDirectionAutoTarget : MortarShootStrategy
    {
        // private MortarCharacterComponent[] _mortars;
        // private Vector2 _currentAimInput;
        
        public MSSDirectionAutoTarget(MortarCharacterComponent mortar, InputAction aimAction, InputAction shootAction) : base(mortar,
            aimAction, shootAction)
        {
            //_mortars = Object.FindObjectsByType<MortarCharacterComponent>(FindObjectsSortMode.None);
        }
        
        // public override void Initialize()
        // {
        //     aimWidget.IsActive = true;
        //     aimWidget.AttachedToTarget = false;
        //     aimWidget.Target = null;
        //     aimWidget.Origin = Vector3.zero;
        //     aimWidget.SetRelativePosition(Vector3.zero);
        //     aimWidget.Hide();
        //     
        //     // Bind input actions
        //     aimAction.performed += OnAiming;
        //     shootAction.performed += OnShoot;
        // }
        //
        // public override void DeInitialize()
        // {
        //     // Unbind input actions
        //     aimAction.performed -= OnAiming;
        //     shootAction.performed -= OnShoot;
        // }
        //
        // private void OnAiming(InputAction.CallbackContext ctx)
        // {
        //     Vector2 aimInput = ctx.ReadValue<Vector2>();
        //
        //     if (aimInput.sqrMagnitude < k_aimDeadZone)
        //         return;
        //     
        //     _currentAimInput = aimInput.normalized;
        // }
        //
        // private void OnShoot(InputAction.CallbackContext ctx)
        // {
        //     // Place the aim widget on the most relevant target
        //     MortarCharacterComponent bestTarget = FindBestTarget();
        //     if (bestTarget == null) return;
        //
        //     aimWidget.SetRelativePosition(bestTarget.transform.position);
        //     
        //     mortar.Shoot();
        // }
        //
        // private MortarCharacterComponent FindBestTarget()
        // {
        //     MortarCharacterComponent best = null;
        //     float bestScore = float.MinValue;
        //
        //     Vector3 mortarPos = mortar.transform.position;
        //
        //     foreach (var target in _mortars)
        //     {
        //         if (target == mortar) continue; // Skip self
        //
        //         Vector3 toTarget = target.transform.position - mortarPos;
        //         float dist = toTarget.magnitude;
        //         if (dist < 0.1f) continue;
        //
        //         Vector2 dir = new Vector2(toTarget.x, toTarget.z) / dist;
        //         float alignment = Vector2.Dot(_currentAimInput, dir) + 1f;
        //
        //         float distanceScore = 1f / (1f + dist);
        //         float totalScore = alignment * 0.7f + distanceScore * 0.3f;
        //
        //         if (totalScore > bestScore)
        //         {
        //             bestScore = totalScore;
        //             best = target;
        //         }
        //     }
        //
        //     return best;
        // }
    }
}