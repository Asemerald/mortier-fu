using System;
using System.Collections.Generic;
using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_Augment", menuName = "Mortier Fu/Augments/New", order = 1)]
    public class SO_Augment : ScriptableObject
    {
        public int ID;
        public string Name;
        
        //stoian
        public string ConditionText;
        public TEMP_STRUCT_AugmentDescription[] Description;
        //stoian
        
        public E_AugmentRarity Rarity;
        public Sprite CardSprite;
        public Sprite SmallSprite;
        
        [Tooltip("The type of augment this is. Must implement IAugment interface.")]
        [TypeFilter(typeof(IAugment))]
        public SerializableType AugmentType;

        [HideInInspector]
        public string ModBundlePath; // chemin vers l'asset bundle si mod
    }
}