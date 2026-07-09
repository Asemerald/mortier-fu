using System;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MortierFu
{
    public class AugmentPickup : MonoBehaviour
    {
        [SerializeField] private E_AugmentRarity _rarity;

        [SerializeField] private AugmentPickupVisual[] _augmentVFXRarityPrototypes;
        [SerializeField] private GameObject[] _pickupVFX;

        private Transform _attachmentPoint;
        private GameObject _vfxInstance;
        
        public E_AugmentRarity Rarity => _rarity;
        public AugmentPickupVisual visual;

        private int _index;

        private AugmentSelectionSystem _system;
        private ShakeService _shakeService;

        private Quaternion _initialRotation;

        public void Initialize(AugmentSelectionSystem system, int augmentIndex, AugmentCardUI augmentCardUI)
        {
            _system = system;
            _index = augmentIndex;
            _shakeService = ServiceManager.Instance.Get<ShakeService>();
            
            _initialRotation = transform.rotation; 
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.attachedRigidbody == null) return;

            if (!other.attachedRigidbody.TryGetComponent(out PlayerCharacter character)) return;
            
            bool success = _system.NotifyPlayerInteraction(character, _index);
              
            if (!success) return;
                
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Augment_Grab, transform.position);
            _shakeService.ShakeController(character.Owner, ShakeService.ShakeType.MID);

            Instantiate(_pickupVFX[(int)_rarity], transform.position, transform.rotation);
            
            Reset();
        }

        public void Reset()
        {
            gameObject.SetActive(false);

            transform.rotation = _initialRotation;
            
            // Set Z rotation to 0
            Vector3 eulerAngles = transform.eulerAngles;
            eulerAngles.z = 0;
            transform.eulerAngles = eulerAngles;
        }

        public void SetAugmentVisual(SO_Augment augment)
        {
            var prototype = GetVFXRarityPrototype(augment.Rarity);

            // Destroy previous visual instance if present
            if (_vfxInstance)
            {
                Destroy(_vfxInstance);
                _vfxInstance = null;
            }

            // Instantiate the visual prefab for this rarity as a child
            _vfxInstance = Instantiate(prototype.gameObject, transform.position, transform.rotation, transform);
            _vfxInstance.transform.localPosition = Vector3.zero;
            _vfxInstance.transform.localRotation = Quaternion.identity;
            _vfxInstance.transform.localScale = Vector3.one;

            visual = _vfxInstance.GetComponent<AugmentPickupVisual>();
            if (visual != null)
            {
                visual.SetLogoSprite(augment.SmallSprite);
            }

            // Adopt the rarity from the prototype
            _rarity = prototype.Rarity;
        }

        private void Update()
        {
            // Set Z rotation to 0
            Vector3 eulerAngles = transform.eulerAngles;
            eulerAngles.z = 0;
            transform.eulerAngles = eulerAngles;
            
            if (!_attachmentPoint) return;
            
            transform.position = _attachmentPoint.position;
        }

        public void AttachToPoint(Transform point)
        {
            _attachmentPoint = point;
            if(point)
                transform.position = point.position;
        }


        public AugmentPickupVisual GetVFXRarityPrototype(E_AugmentRarity rarity)
        {
            foreach (var prototype in _augmentVFXRarityPrototypes)
            {
                if (rarity != prototype.Rarity) continue;

                return prototype;
            }

            throw new Exception($"Prototype not found for rarity {rarity}");
        }
    }
}