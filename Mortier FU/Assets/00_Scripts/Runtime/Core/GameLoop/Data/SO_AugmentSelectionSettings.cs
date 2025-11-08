using UnityEngine.AddressableAssets;
using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_AugmentSelectionSettings", menuName = "Mortier Fu/Settings/AugmentSelection")]
    public class SO_AugmentSelectionSettings : SO_SystemSettings
    {
        [Header("Parameters")]
        [Tooltip("Time before re-enabling player input after augment showcase.")]
        public float EnablePlayerInputDelay = 2f;
        [Tooltip("Time before launch showcase.")]
        public float LaunchShowcaseDelay = 2f;
        [Tooltip("Scale of the augment cards during showcase.")]
        public float CardScale = 4f;
        [Tooltip("Offset between augment cards during showcase.")]
        public float Offset = 2.2f;
        [Tooltip("Duration of the scale animation during showcase.")]
        public float ScaleDuration = 1.3f;
        [Tooltip("Delay before placing augments in the level.")]
        public float PlaceAugmentsDelay = 2f;
        
        [Header("References")]
        public AssetReferenceGameObject AugmentPickupPrefab;
    }
}

