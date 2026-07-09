using System.Collections.Generic;
using UnityEngine;
namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_AugmentProviderSettings", menuName = "Mortier Fu/Settings/Augment Provider")]
    public class SO_AugmentProviderSettings : SO_SystemSettings
    {
        [Header("Settings")]
        public bool AllowCopiesInBatch = false;
        
        public float DropRateDamping = 0.03f;
        
        [Range(0f, 1f)]
        public float DampingRecoveryRate = 0.02f;

        [Header("Drop Rates")] // Drop rate de chaque augment
        public List<LootTable<E_AugmentRarity>.LootTableEntry> RarityDropRates;

        [Header("Rarity Damping")] // Damping apr rareté pour mieux controller 
        public List<RarityDampingEntry> RarityDropRateDamping;

        [Header("Round Locks")] // Permet de lock une augment sur certains round 
        public List<RarityRoundLock> RarityRoundLocks;
    }

    [System.Serializable]
    public class RarityDampingEntry
    {
        public E_AugmentRarity Rarity;
        [Range(0f, 1f)]
        public float DampingFactor = 0.05f;
    }

    [System.Serializable]
    public class RarityRoundLock
    {
        public E_AugmentRarity Rarity;
        public int MinRound = 1; // Permet de lock une augment pour certains rounds
    }
}
