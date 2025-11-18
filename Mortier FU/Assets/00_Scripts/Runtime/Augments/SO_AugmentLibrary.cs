using System.Collections.Generic;
using UnityEngine;

namespace MortierFu
{
    /// <summary>
    /// Used to serialize in the inspector and feed the loot table entries.
    /// </summary>
    [CreateAssetMenu(fileName = "DA_AugmentLibrary", menuName = "Mortier Fu/Augments/New Library")]
    public class SO_AugmentLibrary : ScriptableObject
    {
        public List<SO_Augment> Augments = new();
    }
}