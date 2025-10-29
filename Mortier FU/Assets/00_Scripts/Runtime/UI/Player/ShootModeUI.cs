using TMPro;
using UnityEngine;

namespace MortierFu
{
    public class ShootModeUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _shootModeText;

        void Start()
        {
            ShootModeManager.OnShootModeChanged += UpdateShootModeText;
            UpdateShootModeText(ShootModeManager.CurrentShootMode);
        }

        private void UpdateShootModeText(ShootMode newMode)
        {
            string modeName = newMode switch
            {
                ShootMode.PositionLimited => "1. Position Limited",
                ShootMode.PositionFree => "1.bis Position Free",
                ShootMode.DirectionMaxDistanceOnly => "2. Direction Max Distance Only",
                ShootMode.DirectionLimited => "2.bis Direction Limited",
                ShootMode.Charge => "3. Charge",
                ShootMode.DirectionAutoTarget => "4. Direction Auto Target",
                _ => "Unknown"
            };
            
            _shootModeText.SetText($"Shoot Mode: <b>{modeName}</b>");
        }
    }
   
}