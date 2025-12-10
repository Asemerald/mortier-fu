using System;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace MortierFu
{
    public class AugmentPickup : MonoBehaviour
    {
        [Serializable]
        private struct RarityData
        {
            public E_AugmentRarity Rarity;
            public Sprite BgSprite;
            public Color NameColor;
        }
        
        [SerializeField] private TextMeshProUGUI _nameTxt;
        [SerializeField] private TextMeshProUGUI _descTxt;
        [SerializeField] private Image _rarityBgImg;
        [SerializeField] private Image _iconImg;
        [SerializeField] private RarityData[] _rarityData;
        
        private AugmentSelectionSystem _system;
        private FaceCamera _faceCamera;
        private int _index;
        
        public void Initialize(AugmentSelectionSystem system, int augmentIndex)
        {
            _system = system;
            _index = augmentIndex;
            
            _faceCamera = GetComponent<FaceCamera>();
        }
        
        public void SetAugmentVisual(SO_Augment augment)
        {
            if (augment == null)
            {
                throw new ArgumentNullException(nameof(augment));
            }

            var data = GetRarityData(augment.Rarity);
            
            _rarityBgImg.sprite = data.BgSprite;
            _iconImg.sprite = augment.Icon;
            _nameTxt.SetText(augment.Name.ToUpper());
            _nameTxt.color = data.NameColor;
            _descTxt.SetText(augment.Description);
        }

        private RarityData GetRarityData(E_AugmentRarity augmentRarity) => Array.Find(_rarityData, data => data.Rarity == augmentRarity);

        public void SetFaceCameraEnabled(bool enable) => _faceCamera.enabled = enable;
        
        void OnTriggerEnter(Collider other)
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