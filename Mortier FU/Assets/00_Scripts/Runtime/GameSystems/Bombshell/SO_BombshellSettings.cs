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

        [Tooltip("Map the bombshell speed to get a simulation speed factor used to reduce the number of iteration to calculate precisely and optimally the impact preview.")]
        public AnimationCurve SimulationSpeedCurve = new AnimationCurve(new[] {
            new Keyframe(.5f, 10f),
            new Keyframe(.75f, 8f),
            new Keyframe(1f, 6f),
            new Keyframe(1.25f, 4.7f),
            new Keyframe(1.5f, 4f),
            new Keyframe(2f, 2.8f),
            new Keyframe(2.5f, 2.2f),
            new Keyframe(3f, 1.8f),
            new Keyframe(3.5f, 1.5f),
            new Keyframe(4f, 1.35f),
            new Keyframe(4.5f, 1.26f),
            new Keyframe(5f, 1.2f),
            new Keyframe(5.5f, 1.15f),
            new Keyframe(6f, 0.97f),
            new Keyframe(7f, 0.84f),
            new Keyframe(8f, 0.68f),
            new Keyframe(9f, 0.6f),
            new Keyframe(10f, 0.52f),
            new Keyframe(11f, 0.47f),
            new Keyframe(12f, 0.42f),
        });
        
        [Header("Ranges")]
        [Tooltip("Min and Max distance to apply to the bombshell height based on the target distance.")]
        public Vector2 BombshellHeightDistance = new Vector2(3f, 15f);
        [Tooltip("Min and Max height to clamp the bombshell height adjustment.")]
        public Vector2 BombshellHeightValue = new Vector2(8f, 12f);
        public AnimationCurve BombshellHeightCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        
        [Header("References")]
        public AssetReferenceGameObject BombshellPrefab;
        public LayerMask WhatIsCollidable;
        public LayerMask WhatIsPreviewable;
    }
}