using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_Augment", menuName = "Mortier Fu/New Augment", order = 1)]
    public class DA_Augment : ScriptableObject
    {
        public string Name;
        [TextArea] public string Description;
        public AugmentRarity Rarity;
        public Sprite Icon;
        
        [Tooltip("The type of augment this is. Must implement IAugment interface.")]
        [TypeFilter(typeof(IAugment))]
        public SerializableType AugmentType;
    }
}