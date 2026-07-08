using System.Collections.Generic;
using UnityEngine;

namespace MortierFu
{
    [System.Serializable]
    public struct AugmentRarityRaceUnlock
    {
        public E_AugmentRarity Rarity;

        [Min(1)] public int UnlockFromRace;
    }

    [CreateAssetMenu(fileName = "DA_AugmentProviderSettings", menuName = "Mortier Fu/Settings/Augment Provider")]
    public class SO_AugmentProviderSettings : SO_SystemSettings
    {
        [Header("Settings")]
        [Tooltip("If enabled, the same augment can appear multiple times in the same augment bag.")]
        public bool AllowCopiesInBatch = false;

        [Tooltip("Reduces an augment's chance after being picked. Lower values make it appear less often.")]
        public float DropRateDamping = 0.03f;

        [Header("Drop Rates")] [Tooltip("The drop rate of every augment rarity.")]
        public List<LootTable<E_AugmentRarity>.LootTableEntry> RarityDropRates;

        [Header("Rarity Unlocks By Race")] public bool UseRarityUnlocksByRace = true;

        [Tooltip("Defines from which augment race each rarity becomes available.")]
        public List<AugmentRarityRaceUnlock> RarityUnlocksByRace = new();

        [Tooltip("If enabled, falls back to the normal rarity table when no unlocked rarity is available.")]
        public bool FallbackToNormalRarityTableIfNoUnlockedRarity = false;
    }
}