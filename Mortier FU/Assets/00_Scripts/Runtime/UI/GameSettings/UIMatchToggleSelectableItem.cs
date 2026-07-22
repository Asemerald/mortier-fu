using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MortierFu
{
    public sealed class UIMatchToggleSelectableItem : UIMatchSelectableItemBase
    {
        [Header("Setting")]
        [SerializeField] private MatchSettingId _settingId;

        [Header("Text")]
        [SerializeField] private TMP_Text _valueText;
        [SerializeField] private string _trueText = "YES";
        [SerializeField] private string _falseText = "NO";

        [Header("Visuals")]
        [SerializeField] private Graphic _toggleGraphic;
        [SerializeField] private Color _falseColor = Color.white;
        [SerializeField] private Color _trueColor = Color.green;
        [SerializeField] private Color _selectedColor = Color.yellow;
        [SerializeField] private Color _disabledColor = Color.gray;

        private bool _isSelected;

        public override void OnMove(AxisEventData eventData)
        {
            if (TryGetHorizontalMove(eventData, out _))
            {
                Toggle();
                eventData.Use();
                return;
            }

            base.OnMove(eventData);
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            Toggle();
            eventData.Use();
        }

        public override void OnSelect(BaseEventData eventData)
        {
            _isSelected = true;
            base.OnSelect(eventData);
            Refresh();
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            _isSelected = false;
            base.OnDeselect(eventData);
            Refresh();
        }

        public override void Refresh()
        {
            bool value = Data && Data.GetBool(_settingId);
            bool editable = Data && Data.IsSettingEditable(_settingId);

            if (_valueText)
                _valueText.text = value ? _trueText : _falseText;

            if (_toggleGraphic)
            {
                if (!editable)
                    _toggleGraphic.color = _disabledColor;
                else if (_isSelected)
                    _toggleGraphic.color = _selectedColor;
                else
                    _toggleGraphic.color = value ? _trueColor : _falseColor;
            }

            SetItemInteractable(editable);
        }

        private void Toggle()
        {
            if (Data == null)
                return;

            if (!Data.IsSettingEditable(_settingId))
            {
                Refresh();
                return;
            }

            bool value = Data.GetBool(_settingId);
            Data.SetBool(_settingId, !value);

            Refresh();
        }
    }
}