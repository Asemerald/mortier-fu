using System;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class AugmentPickup : MonoBehaviour
    {
        [SerializeField] private E_AugmentRarity _rarity;

        [SerializeField] private ParticleSystem _dissolveColor01;
        [SerializeField] private ParticleSystem _dissolveColor02;
        [SerializeField] private ParticleSystem _roundColor01;
        [SerializeField] private ParticleSystem _roundColor02;
        [SerializeField] private MeshRenderer _planeMeshRenderer;
        [SerializeField] private ParticleSystem _logoParticleSystem;
        [SerializeField] private AugmentPickup[] _augmentVFXRarityPrototypes;

        private Transform _attachmentPoint;
        
        public E_AugmentRarity Rarity => _rarity;

        private int _index;

        private AugmentSelectionSystem _system;
        private ShakeService _shakeService;

        private Quaternion _initialRotation;

        public void Initialize(AugmentSelectionSystem system, int augmentIndex)
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
            _logoParticleSystem.textureSheetAnimation.SetSprite(0, augment.SmallSprite);

            var prototype = GetVFXRarityPrototype(augment.Rarity);
            ConfigureAsClone(prototype);
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

        // Prototype pattern
        public void ConfigureAsClone(AugmentPickup source)
        {
            _rarity = source._rarity;

            if (_dissolveColor01 && source._dissolveColor01)
            {
                var main = _dissolveColor01.main;
                main.startColor = source._dissolveColor01.main.startColor;
            }

            if (_dissolveColor02 && source._dissolveColor02)
            {
                var main = _dissolveColor02.main;
                main.startColor = source._dissolveColor02.main.startColor;
            }

            if (_roundColor01 && source._roundColor01)
            {
                var main = _roundColor01.main;
                main.startColor = source._roundColor01.main.startColor;
            }

            if (_roundColor02 && source._roundColor02)
            {
                var main = _roundColor02.main;
                main.startColor = source._roundColor02.main.startColor;
            }

            if (_planeMeshRenderer && source._planeMeshRenderer)
            {
                _planeMeshRenderer.sharedMaterial = source._planeMeshRenderer.sharedMaterial;
            }
        }

        public AugmentPickup GetVFXRarityPrototype(E_AugmentRarity rarity)
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