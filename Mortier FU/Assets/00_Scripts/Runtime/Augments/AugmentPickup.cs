using UnityEngine;

namespace MortierFu
{
    public class AugmentPickup : MonoBehaviour
    {
        private int _index;
        
        private AugmentSelectionSystem _system;
        
        public void Initialize(AugmentSelectionSystem system, int augmentIndex)
        {
            _system = system;
            _index = augmentIndex;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.attachedRigidbody == null) return;

            if (other.attachedRigidbody.TryGetComponent(out PlayerCharacter character))
            {
                bool success = _system.NotifyPlayerInteraction(character, _index);
                if (success == false) return;

                Hide();
            }
        }
        
        public void Hide() => gameObject.SetActive(false);
    }
}
