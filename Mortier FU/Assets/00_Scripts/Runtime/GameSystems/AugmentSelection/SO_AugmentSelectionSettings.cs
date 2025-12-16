using MortierFu.Shared;
using NaughtyAttributes;
using UnityEngine.AddressableAssets;
using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_AugmentSelectionSettings", menuName = "Mortier Fu/Settings/Augment Selection")]
    public class SO_AugmentSelectionSettings : SO_SystemSettings
    {
        [Header("Settings")]
        public bool EnforceAugmentCount = false;
        [ShowIf("EnforceAugmentCount")]
        public int ForcedAugmentCount = 5;
        
        [Header("Timing")]
        [Tooltip("Delay before starting the augment showcase (in seconds).")]
        public float ShowcaseStartDelay = 2f;

        [Tooltip("Time it takes for each card to scale up from zero when the showcase starts.")]
        public float CardPopInDuration = 1.3f;
        
        [Tooltip("Time between each card growth")]
        public MinMaxRange CardPopInStagger = new(0.08f, 0.15f);

        [Tooltip("Delay before restoring player input after all animations have finished.")]
        public float PlayerInputReenableDelay = 3f;

        [Header("Card Animation Ranges")]
        [Tooltip("Randomized duration range for the movement and scaling animation when each card moves to its target position.")]
        public MinMaxRange CardMoveDurationRange = new(0.4f, 0.8f);

        [Tooltip("Randomized delay range between each card’s movement animation (stagger).")]
        public MinMaxRange CardMoveStaggerRange = new(0.12f, 0.4f);

        [Header("Visuals")]
        [Tooltip("Final scale applied to each augment card during showcase. Size is decided based on augment count.")]
        public MinMaxRange DisplayedCardScaleRange = new(4f, 3f);
        [Tooltip("Final scale applied to each augment card during carousel.")]
        public float CarouselCardScale = 1.5f;

        [Tooltip("Horizontal distance between augment cards during showcase. Spacing is decided based on augment count.")]
        public MinMaxRange CardSpacingRange = new(2.2f, 1f);
        
        [Header("References")]
        public AssetReferenceGameObject AugmentPickupPrefab;

    }
}

