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
        [SerializeField] private GameObject[] _displayVFX;
        public E_AugmentRarity Rarity => _rarity;

        public void HideVfx()
        {
            foreach (GameObject vfx in _displayVFX)
            {
                vfx.SetActive(false);
            }
        }

        public void SetLogoSprite(Sprite sprite)
        {
            if (_logoParticleSystem == null || sprite == null) return;
            _logoParticleSystem.textureSheetAnimation.SetSprite(0, sprite);
        }

        public void SetVfx()
        {
            foreach (GameObject vfx in _displayVFX)
            {
                vfx.SetActive(true);
            }
        }
    }
}
