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

    [System.Serializable]
    public struct AugmentRarityDamping
    {
        public E_AugmentRarity Rarity;

        public float DampingFactor;
    }

    [CreateAssetMenu(fileName = "DA_AugmentProviderSettings", menuName = "Mortier Fu/Settings/Augment Provider")]
    public class SO_AugmentProviderSettings : SO_SystemSettings
    {
        [Header("Settings")]
        [Tooltip("Si activé, il peut y avoir plusieurs copies d'une augment dans un tas.")]
        public bool AllowCopiesInBatch = false;

        [Tooltip("Réduit les chances d'une augment d'être pick.")]
        public float DropRateDamping = 0.03f;

        [Header("Drop Rates")] [Tooltip("The drop rate of every augment rarity.")]
        public List<LootTable<E_AugmentRarity>.LootTableEntry> RarityDropRates;

        [Header("Rarity Damping")]
        [Tooltip("Per-rarity damping factor.")]
        public List<AugmentRarityDamping> RarityDropRateDamping = new();

        [Tooltip("How much an augment's chance recovers towards 1 at the start of each new race/round.")]
        public float DampingRecoveryRate = 0.02f;

        [Header("Rarity Unlocks By Race")] public bool UseRarityUnlocksByRace = true;

        [Tooltip("Defines from which augment race each rarity becomes available.")]
        public List<AugmentRarityRaceUnlock> RarityUnlocksByRace = new();

        [Tooltip("If enabled, falls back to the normal rarity table when no unlocked rarity is available.")]
        public bool FallbackToNormalRarityTableIfNoUnlockedRarity = false;
    }
}