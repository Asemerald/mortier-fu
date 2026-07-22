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
        [TextArea(2, 5)]
        public string ConditionText;
        public TEMP_STRUCT_AugmentDescription[] Description;
        //stoian
        
        public E_AugmentRarity Rarity;
        public Sprite CardSprite;
        public Sprite SmallSprite;
        
        [TypeFilter(typeof(IAugment))]
        public SerializableType AugmentType;
        
        public GameObject AugmentVFX;

        [HideInInspector]
        public string ModBundlePath; // chemin vers l'asset bundle si mod
    }
}