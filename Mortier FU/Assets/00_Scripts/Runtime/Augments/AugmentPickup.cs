using UnityEngine;

namespace MortierFu
{
    public class AugmentPickup : MonoBehaviour {
        [SerializeField] private E_AugmentRarity _rarity;
        
        [SerializeField] private ParticleSystem _dissolveColor01;
        [SerializeField] private ParticleSystem _roundColor01;
        [SerializeField] private ParticleSystem _roundColor02;
        [SerializeField] private MeshRenderer _planeMeshRenderer;

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

            if (other.attachedRigidbody.TryGetComponent(out PlayerCharacter character))
            {
                bool success = _system.NotifyPlayerInteraction(character, _index);
                AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Augment_Grab, transform.position);
                _shakeService.ShakeController(character.Owner, ShakeService.ShakeType.MID);
                if (!success) return;

                Reset();
            }
        }

        public void Reset()
        {
            gameObject.SetActive(false);
            
            transform.rotation = _initialRotation;
        }
        
        // Prototype pattern
        public void ConfigureAsClone(AugmentPickup source) {
            _rarity = source._rarity;

            if (_dissolveColor01 && source._dissolveColor01) {
                var main = _dissolveColor01.main;
                main.startColor = source._dissolveColor01.main.startColor;
            }
            
            if (_roundColor01 && source._roundColor01) {
                var main = _roundColor01.main;
                main.startColor = source._roundColor01.main.startColor;
            }

            if (_roundColor02 && source._roundColor02) {
                var main = _roundColor02.main;
                main.startColor = source._roundColor02.main.startColor;
            }

            if (_planeMeshRenderer && source._planeMeshRenderer) {
                _planeMeshRenderer.sharedMaterial = source._planeMeshRenderer.sharedMaterial;
            }
        }
    }
}