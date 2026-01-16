using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(menuName = "Mortier Fu/UI/RaritySpritesFactory", fileName = "DA_RaritySpritesFactory")]
    public class SO_RaritySpritesFactory : ScriptableObject
    {
        [SerializeField] private RaritySpritesEntry[] _spritesPerRarity;
        
        public Sprite GetRarityBgSpriteFromRarity(E_AugmentRarity rarity)
        {
            foreach (var entry in _spritesPerRarity)
            {
                if (entry.Rarity == rarity)
                {
                    return entry.BgSprite;
                }
            }

            Debug.LogWarning($"RarityBgSpriteFactory: No background sprite found for rarity {rarity}. Returning null.");
            return null;
        }

        public Sprite GetRarityBorderSpriteFromRarity(E_AugmentRarity rarity)
        {
            foreach (var entry in _spritesPerRarity)
            {
                if (entry.Rarity == rarity)
                {
                    return entry.BorderSprite;
                }
            }

            Debug.LogWarning($"RarityBgSpriteFactory: No border sprite found for rarity {rarity}. Returning null.");
            return null;
        }
        
        [System.Serializable]
        private struct RaritySpritesEntry
        {
            public E_AugmentRarity Rarity;
            public Sprite BorderSprite;
            public Sprite BgSprite;
        }
    }
}
