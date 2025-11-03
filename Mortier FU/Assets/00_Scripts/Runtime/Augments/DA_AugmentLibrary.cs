using System.Collections.Generic;
using UnityEngine;

namespace MortierFu
{
    /// <summary>
    /// Used to serialize in the inspector and feed the loot table entries.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [CreateAssetMenu(fileName = "DA_AugmentLibrary", menuName = "Mortier Fu/New Augment Library")]
    public class DA_AugmentLibrary : ScriptableObject
    {
        public List<LootTable<DA_Augment>.LootTableEntry> AugmentEntries = new();
    }
}