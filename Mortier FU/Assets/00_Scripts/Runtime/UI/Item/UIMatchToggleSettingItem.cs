using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MortierFu
{
    public sealed class UIMatchToggleSettingItem : UIMatchSettingsItemBase
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

        private bool _selected;

        public override bool HandleHorizontal(int direction)
        {
            if (direction == 0)
                return false;

            Toggle();
            return true;
        }

        public override bool HandleSubmit()
        {
            Toggle();
            return true;
        }

        protected override void OnSelectionChanged(bool selected)
        {
            _selected = selected;
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
                else if (_selected)
                    _toggleGraphic.color = _selectedColor;
                else
                    _toggleGraphic.color = value ? _trueColor : _falseColor;
            }

            SetReadOnlyVisual(editable);
        }

        private void Toggle()
        {
            if (Data == null)
                return;

            if (!Data.IsSettingEditable(_settingId))
                return;

            bool value = Data.GetBool(_settingId);
            Data.SetBool(_settingId, !value);

            Refresh();
        }
    }
}