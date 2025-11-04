using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace MortierFu
{
    public class AugmentPickup : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _augmentNameText;
        [SerializeField] private Image _augmentIconImage;
        [SerializeField] private MeshRenderer _bgMeshRenderer;
        
        private AugmentSelectionSystem _system;
        private int _index;
        
        public void Initialize(AugmentSelectionSystem system, int augmentIndex)
        {
            _system = system;
            _index = augmentIndex;
        }
        
        public void SetAugmentVisual(DA_Augment augment)
        {
            _augmentNameText.text = augment != null ? augment.Name : "None";
            _augmentIconImage.sprite = augment != null ? augment.Icon : null;
            _bgMeshRenderer.material.color = augment != null ? augment.BgColor : Color.magenta;
            
            Show();
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if(other.attachedRigidbody == null) return;
            
            if (other.attachedRigidbody.TryGetComponent(out PlayerCharacter character))
            {
                bool success = _system.NotifyPlayerInteraction(character, _index);
                if(success == false) return;
            
                Hide();
            }
        }
        
        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}