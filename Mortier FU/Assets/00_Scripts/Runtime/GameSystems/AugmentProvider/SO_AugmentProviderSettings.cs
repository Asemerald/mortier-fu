using System.Collections.Generic;
using UnityEngine;
namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_AugmentProviderSettings", menuName = "Mortier Fu/Settings/Augment Provider")]
    public class SO_AugmentProviderSettings : SO_SystemSettings
    {
        [Header("Settings")]
        [Tooltip("If enabled, the same augment can appear multiple times in the same augment bag.")]
        public bool AllowCopiesInBatch = false;
        
        [Header("Drop Rates")]
        [Tooltip("The drop rate of every augment rarities.")]
        public List<LootTable<E_AugmentRarity>.LootTableEntry> RarityDropRates;

    }
}
