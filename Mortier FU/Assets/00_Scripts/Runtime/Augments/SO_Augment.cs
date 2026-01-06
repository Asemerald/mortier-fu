using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_Augment", menuName = "Mortier Fu/Augments/New", order = 1)]
    public class SO_Augment : ScriptableObject
    {
        public string Name;
        [TextArea] public string Description;
        public E_AugmentRarity Rarity;
        public Sprite Icon;
        public Sprite RarityIcon;
        public Sprite BackIcon;
        public Sprite LogoIcon;
        
        [Tooltip("The type of augment this is. Must implement IAugment interface.")]
        [TypeFilter(typeof(IAugment))]
        public SerializableType AugmentType;

        [HideInInspector]
        public string ModBundlePath; // chemin vers l'asset bundle si mod
    }
}