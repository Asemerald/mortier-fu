using System;
using UnityEngine;

namespace MortierFu
{
    public abstract class AugmentVisual : MonoBehaviour
    {
        [SerializeField] private AugmentPickupVisual[] _augmentVFXRarityPrototypes;
        private GameObject _vfxInstance;
        
        protected AugmentPickupVisual GetVFXRarityPrototype(E_AugmentRarity rarity)
        {
            foreach (AugmentPickupVisual prototype in _augmentVFXRarityPrototypes)
            {
                if (rarity != prototype.Rarity)
                    continue;

                return prototype;
            }

            throw new Exception($"Prototype not found for rarity {rarity}");
        }

        protected GameObject SetAugmentVisualIcon(SO_Augment augment, Vector3 position, Quaternion rotation, Transform parent, Vector3 scale, bool hideVfx = false)
        {
            AugmentPickupVisual prototype = GetVFXRarityPrototype(augment.Rarity);
            _vfxInstance = Instantiate(prototype.gameObject, position,rotation , parent);
            _vfxInstance.transform.localPosition = position;
            _vfxInstance.transform.localRotation =rotation;
            _vfxInstance.transform.localScale = scale;

            AugmentPickupVisual _visualInstance = _vfxInstance.GetComponent<AugmentPickupVisual>();

            if(hideVfx)
                _visualInstance.HideVfx();
            
            if (_visualInstance)
                _visualInstance.SetLogoSprite(augment.SmallSprite);
            
            return _vfxInstance;
        }
    }
}
