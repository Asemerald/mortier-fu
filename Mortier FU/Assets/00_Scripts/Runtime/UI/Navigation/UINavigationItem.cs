using UnityEngine;
using UnityEngine.UI;

namespace MortierFu
{
    public abstract class UINavigationItem : MonoBehaviour
    {
        [Header("Selection Visual")]
        [SerializeField] private Graphic _selectionGraphic;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _selectedColor = Color.yellow;

        public virtual bool IsAvailable => isActiveAndEnabled;

        public void SetSelected(bool selected)
        {
            if (_selectionGraphic)
            {
                _selectionGraphic.color = selected ? _selectedColor : _normalColor;
            }

            OnSelectionChanged(selected);
        }

        public virtual bool HandleHorizontal(int direction) => false;

        public virtual bool HandleSubmit() => false;

        public virtual bool HandleCancel() => false;

        protected virtual void OnSelectionChanged(bool selected)
        { }
    }
}