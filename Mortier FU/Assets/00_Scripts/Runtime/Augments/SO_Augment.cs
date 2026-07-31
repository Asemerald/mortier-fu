using UnityEngine;
using UnityEngine.Serialization;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_Augment", menuName = "Mortier Fu/Augments/New", order = 1)]
    public class SO_Augment : ScriptableObject
    {
        public int ID;
        public string Name;
        
        [Header("Description")]
        [TextArea(2, 5)]
        public string ConditionText;
        public AugmentDescription[] Description;
        public float DescFontSize;
        
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