using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_BombshellSettings", menuName = "Mortier Fu/Settings/Bombshell")]
    public class SO_BombshellSettings : SO_SystemSettings
    {
        [Header("Parameters")]
        [Tooltip("Determine if the players can damage themselves with their own bombshells.")]
        public bool AllowSelfDamage = true;
        [Tooltip("Determine if the bombshells of the same player can collide together.")]
        public bool DisableBombshellSelfCollision = true;
        public float BombshellHeight = 8f;
        [Tooltip("This will wait n% of the travel time before showing the impact preview for the remaining 100-n% of the preview growing before impact.")]
        [Range(0f, 1f)]
        public float ImpactPreviewDelayFactor = 0.6f;

        [Header("References")]
        public AssetReferenceGameObject BombshellPrefab;
    }
}