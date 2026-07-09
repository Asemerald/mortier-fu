using UnityEngine;
using PrimeTween;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "SO_AugmentSummaryUISettings", menuName = "Mortier Fu/UI/Augment Summary UI Settings")]
    public class SO_AugmentSummaryUISettings : ScriptableObject
    {
        [Header("Prefabs")]
        public GameObject PlayerImage;
        public GameObject RarityIcon;
        public GameObject Card;
        public int RarityIconCount = 9;

        [Header("Player Animation Settings")]
        public float PlayerScaleDuration = 0.4f;
        public float PlayerTargetScale = 0.65f;
        public float PlayerAnimDelay = 0.3f;
        public Ease PlayerScaleEase = Ease.OutBack;

        [Header("Augment Icon Animation")]
        public float AugmentIconRadius = 225f;
        public float ChildAnimDelay = 0.3f;
        public float AugmentIconAnimDuration = 0.8f;
        public Ease AugmentIconScaleEase = Ease.OutBack;
        public Ease AugmentIconMoveEase = Ease.OutCubic;
        public float ChildAnimationExponentFactor = 1.2f;

        [Header("Card Icon Animation")] 
        public float CardDurationScale = 0.4f;
        public Ease CardScaleEase = Ease.OutBack;
        public float CardScaleMultiplier = 1f;
        
        [Header("Player Icon Visuals")]
        [Tooltip("Order : Blue, Red, Green, Yellow")] public Sprite[] PlayerIcons;

        public Sprite GetPlayerIconByPlayerIndex(int index)
        {
            return PlayerIcons[index];
        }
    }
}