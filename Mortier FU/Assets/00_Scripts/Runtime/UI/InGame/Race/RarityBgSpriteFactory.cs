using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(menuName = "Mortier Fu/UI/RarityBgSpriteFactory", fileName = "DA_RarityBgSpriteFactory")]
    public class RarityBgSpriteFactory : ScriptableObject
    {
        [SerializeField] private RarityBgSpriteEntry[] _bgSpritePerRarity;
        
        public Sprite GetRarityBgSpriteFromRarity(E_AugmentRarity rarity)
        {
            foreach (var entry in _bgSpritePerRarity)
            {
                if (entry.Rarity == rarity)
                {
                    return entry.BgSprite;
                }
            }

            Debug.LogWarning($"RarityBgSpriteFactory: No background sprite found for rarity {rarity}. Returning null.");
            return null;
        }
        
        [System.Serializable]
        private struct RarityBgSpriteEntry
        {
            public E_AugmentRarity Rarity;
            public Sprite BgSprite;
        }
    }
}
