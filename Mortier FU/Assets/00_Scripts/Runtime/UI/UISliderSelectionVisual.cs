using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MortierFu
{
    public sealed class UISliderSelectionVisual : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        [Header("References")]
        [SerializeField] private Image _handleImage;

        [Header("Sprites")]
        [SerializeField] private Sprite _normalHandleSprite;
        [SerializeField] private Sprite _selectedHandleSprite;
        [SerializeField] private Sprite _editingHandleSprite;

        private bool _isEditing;

        private void Awake() => ApplyNormalVisual();

        private void OnDisable()
        {
            _isEditing = false;
            ApplyNormalVisual();
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (_isEditing)
                ApplyEditingVisual();
            else
                ApplySelectedVisual();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _isEditing = false;
            ApplyNormalVisual();
        }

        public void SetEditing(bool editing)
        {
            if (_isEditing == editing)
                return;

            _isEditing = editing;

            if (_isEditing)
                ApplyEditingVisual();
            else
                ApplySelectedVisual();
        }

        public void ApplySelectedVisual() => ApplySprite(_selectedHandleSprite);

        public void ApplyEditingVisual()
        {
            Sprite sprite = _editingHandleSprite ? _editingHandleSprite : _selectedHandleSprite;
            ApplySprite(sprite);
        }

        public void ApplyNormalVisual() => ApplySprite(_normalHandleSprite);

        private void ApplySprite(Sprite sprite)
        {
            if (!_handleImage || !sprite)
                return;

            _handleImage.sprite = sprite;
            _handleImage.SetNativeSize();
        }
    }
}