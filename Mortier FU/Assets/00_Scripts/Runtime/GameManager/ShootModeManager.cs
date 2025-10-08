using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public static class ShootModeManager
    {
        public static Action<ShootMode> OnShootModeChanged;
        
        private static ShootMode _currentShootMode = ShootMode.PositionLimited;
        private static Mortar[] _mortars;
        
        public static ShootMode CurrentShootMode => _currentShootMode;
        
        public static void CycleShootMode(InputAction.CallbackContext ctx)
        {
            int modeCount = Enum.GetValues(typeof(ShootMode)).Length;
            var newShootMode = (ShootMode)(((int)_currentShootMode + 1) % modeCount);
            _currentShootMode = newShootMode;
            
            SetShootMode(newShootMode);
        }

        public static void SetShootMode(ShootMode newShootMode)
        {
            if (_mortars == null)
            {
                _mortars = UnityEngine.Object.FindObjectsByType<Mortar>(FindObjectsSortMode.None);
            }
            
            foreach (var mortar in _mortars)
            {
                mortar.SetShootMode(newShootMode);
                OnShootModeChanged?.Invoke(newShootMode);
            }
        }
    }
}