using System;
using MortierFu.Shared;
using UnityEngine;
using Object = UnityEngine.Object;

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
        [SerializeField] private Light _light;
        [SerializeField] private AugmentPickup[] _augmentVFXRarityPrototypes;
        [SerializeField] private GameObject[] _pickupVFX;

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
                
                _dissolveColor01.gameObject.SetActive(source._dissolveColor01.gameObject.activeSelf);
                _dissolveColor01.transform.localScale = source._dissolveColor01.transform.localScale;
            }

            if (_dissolveColor02 && source._dissolveColor02)
            {
                var main = _dissolveColor02.main;
                main.startColor = source._dissolveColor02.main.startColor;
                
                _dissolveColor02.gameObject.SetActive(source._dissolveColor02.gameObject.activeSelf);
                _dissolveColor02.transform.localScale = source._dissolveColor02.transform.localScale;
            }

            if (_roundColor01 && source._roundColor01)
            {
                var main = _roundColor01.main;
                main.startColor = source._roundColor01.main.startColor;
                
                _roundColor01.gameObject.SetActive(source._roundColor01.gameObject.activeSelf);
            }

            if (_roundColor02 && source._roundColor02)
            {
                var main = _roundColor02.main;
                main.startColor = source._roundColor02.main.startColor;
                
                _roundColor02.gameObject.SetActive(source._roundColor02.gameObject.activeSelf);
            }

            if (_planeMeshRenderer && source._planeMeshRenderer)
            {
                _planeMeshRenderer.sharedMaterial = source._planeMeshRenderer.sharedMaterial;
                
                _planeMeshRenderer.transform.localScale = source._planeMeshRenderer.transform.localScale;
            }
            
            if (_light && source._light)
            {
                _light.color = source._light.color;
                _light.intensity = source._light.intensity;
                _light.areaSize = source._light.areaSize;
                
                _light.gameObject.SetActive(source._light.gameObject.activeSelf);
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