using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(menuName = "Mortier Fu/UI/RaritySpritesFactory", fileName = "DA_RaritySpritesFactory")]
    public class SO_RaritySpritesFactory : ScriptableObject
    {
        [SerializeField] private RaritySpritesEntry[] _spritesPerRarity;
        
        public Sprite GetAugmentIconSpriteFromRarity(E_AugmentRarity rarity)
        {
            foreach (var entry in _spritesPerRarity)
            {
                if (entry.Rarity == rarity)
                {
                    return entry.AugmentIcon;
                }
            }

            Debug.LogWarning($"RarityBgSpriteFactory: No background sprite found for rarity {rarity}. Returning null.");
            return null;
        }

        /*public Texture GetTitleRarityFilter(E_AugmentRarity rarity)
        {
            foreach (var entry in _spritesPerRarity)
            {
                if (entry.Rarity == rarity)
                {
                    return entry.TitleFilter;
                }
            }

            Debug.LogWarning($"RarityBgSpriteFactory: No background sprite found for rarity {rarity}. Returning null.");
            return null;
        }*/

        public Sprite GetAugmentCardSpriteFromRarity(E_AugmentRarity rarity)
        {
            foreach (var entry in _spritesPerRarity)
            {
                if (entry.Rarity == rarity)
                {
                    return entry.AugmentCard;
                }
            }

            Debug.LogWarning($"RarityBgSpriteFactory: No border sprite found for rarity {rarity}. Returning null.");
            return null;
        }
        
        public Sprite GetAugmentCardBgSpriteFromRarity(E_AugmentRarity rarity)
        {
            foreach (var entry in _spritesPerRarity)
            {
                if (entry.Rarity == rarity)
                {
                    return entry.AugmentCardBackground;
                }
            }

            Debug.LogWarning($"RarityBgSpriteFactory: No border sprite found for rarity {rarity}. Returning null.");
            return null;
        }
        
        public GameObject GetRarityVfxFromRarity(E_AugmentRarity rarity)
        {
            foreach (var entry in _spritesPerRarity)
            {
                if (entry.Rarity == rarity)
                {
                    return entry.cardVfx;
                }
            }

            Debug.LogWarning($"RarityBgSpriteFactory: No vfx found for rarity {rarity}. Returning null.");
            return null;
        }
        
        [System.Serializable]
        private struct RaritySpritesEntry
        {
            public E_AugmentRarity Rarity;
            public Sprite AugmentCard;
            public Sprite AugmentIcon;
            public Sprite AugmentCardBackground;
           // public Texture TitleFilter;
            public GameObject cardVfx;
        }
    }
}
