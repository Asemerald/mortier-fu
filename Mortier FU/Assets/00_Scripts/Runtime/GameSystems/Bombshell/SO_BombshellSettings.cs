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
        public float BounceSpeedDampingFactor = 0.95f;
        public float BounceDamageDampingFactor = 0.5f;
        [Tooltip("This will wait n% of the travel time before showing the impact preview for the remaining 100-n% of the preview growing before impact.")]
        [Range(0f, 1f)]
        public float ImpactPreviewDelayFactor = 0.6f;

        [Header("Ranges")]
        [Tooltip("Min and Max distance to apply to the bombshell height based on the target distance.")]
        public Vector2 BombshellHeightDistance = new Vector2(3f, 15f);
        [Tooltip("Min and Max height to clamp the bombshell height adjustment.")]
        public Vector2 BombshellHeightValue = new Vector2(8f, 12f);
        public AnimationCurve BombshellHeightCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        
        [Header("References")]
        public AssetReferenceGameObject BombshellPrefab;
        public LayerMask WhatIsCollidable;

    }
}