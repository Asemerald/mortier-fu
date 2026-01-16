using UnityEngine;

namespace MortierFu
{
    public class AugmentPickup : MonoBehaviour
    {
        private int _index;

        private AugmentSelectionSystem _system;

        private Quaternion _initialRotation;

        public void Initialize(AugmentSelectionSystem system, int augmentIndex)
        {
            _system = system;
            _index = augmentIndex;

            _initialRotation = transform.rotation;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.attachedRigidbody == null) return;

            if (other.attachedRigidbody.TryGetComponent(out PlayerCharacter character))
            {
                bool success = _system.NotifyPlayerInteraction(character, _index);
                AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Augment_Grab, transform.position);
                ShakeService.ShakeController(character.Owner, ShakeService.ShakeType.MID);
                if (!success) return;

                Reset();
            }
        }

        public void Reset()
        {
            gameObject.SetActive(false);
            
            transform.rotation = _initialRotation;
        }
    }
}