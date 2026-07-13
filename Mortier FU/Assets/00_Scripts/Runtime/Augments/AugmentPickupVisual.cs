using System;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    // Separate visual/decoration component for augment pickups.
    public class AugmentPickupVisual : MonoBehaviour
    {
        [SerializeField] private E_AugmentRarity _rarity;
        [SerializeField] private ParticleSystem _logoParticleSystem;
        public E_AugmentRarity Rarity => _rarity;
        

        public void SetLogoSprite(Sprite sprite)
        {
            if (_logoParticleSystem == null || sprite == null) return;
            _logoParticleSystem.textureSheetAnimation.SetSprite(0, sprite);
        }
        
    }
}
