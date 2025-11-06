using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace MortierFu
{
    public class AugmentPickup : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _augmentNameText;
        [SerializeField] private TextMeshProUGUI _augmentDescriptionText;
        [SerializeField] private Image _augmentIconImage;
        [SerializeField] private MeshRenderer _bgMeshRenderer;
        private FaceCamera _faceCamera;
        
        private AugmentSelectionSystem _system;
        private int _index;
        
        public void Initialize(AugmentSelectionSystem system, int augmentIndex)
        {
            _system = system;
            _index = augmentIndex;
            
            _faceCamera = GetComponent<FaceCamera>();
        }
        
        public void SetAugmentVisual(SO_Augment augment)
        {
            _augmentNameText.text = augment != null ? augment.Name : "None";
            _augmentDescriptionText.text = augment != null ? augment.Description : "None";
            _augmentIconImage.sprite = augment != null ? augment.Icon : null;
            _bgMeshRenderer.material.color = augment != null ? augment.BgColor : Color.magenta;
        }

        public void SetFaceCameraEnabled(bool enable) => _faceCamera.enabled = enable;
        
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