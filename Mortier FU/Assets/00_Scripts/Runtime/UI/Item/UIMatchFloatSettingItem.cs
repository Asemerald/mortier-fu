using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MortierFu
{
    public sealed class UIMatchFloatSettingItem : UIMatchSettingsItemBase
    {
        [Header("Setting")]
        [SerializeField] private MatchSettingId _settingId = MatchSettingId.RaceTimeLimit;

        [Header("Value")]
        [SerializeField] private float _minValue = 0.5f;
        [SerializeField] private float _maxValue = 3f;
        [SerializeField] private float _step = 0.1f;
        [SerializeField] private bool _wrapValue;

        [Header("Format")]
        [SerializeField] private string _prefix = "";
        [SerializeField] private string _suffix = "x";
        [SerializeField] private string _format = "0.#";

        [Header("Visuals")]
        [SerializeField] private TMP_Text _valueText;
        [SerializeField] private Graphic _leftArrow;
        [SerializeField] private Graphic _rightArrow;
        [SerializeField] private Color _normalArrowColor = Color.white;
        [SerializeField] private Color _selectedArrowColor = Color.yellow;
        [SerializeField] private Color _usedArrowColor = Color.green;
        [SerializeField] private Color _disabledArrowColor = Color.gray;

        private bool _selected;
        private int _lastDirection;

        public override bool HandleHorizontal(int direction)
        {
            if (direction == 0 || Data == null)
                return false;

            if (!Data.IsSettingEditable(_settingId))
                return true;

            float currentValue = Data.GetFloat(_settingId);
            float nextValue = currentValue + direction * Mathf.Max(0.01f, _step);

            if (_wrapValue)
                nextValue = WrapValue(nextValue);
            else
                nextValue = Mathf.Clamp(nextValue, _minValue, _maxValue);

            Data.SetFloat(_settingId, nextValue);

            _lastDirection = direction;
            Refresh();

            return true;
        }

        protected override void OnSelectionChanged(bool selected)
        {
            _selected = selected;

            if (!selected)
                _lastDirection = 0;

            Refresh();
        }

        public override void Refresh()
        {
            float value = Data ? Data.GetFloat(_settingId) : 0f;
            bool editable = Data && Data.IsSettingEditable(_settingId);

            if (_valueText)
                _valueText.text = $"{_prefix}{value.ToString(_format)}{_suffix}";

            SetReadOnlyVisual(editable);
            UpdateArrow(_leftArrow, _lastDirection < 0, editable);
            UpdateArrow(_rightArrow, _lastDirection > 0, editable);
        }

        private void UpdateArrow(Graphic arrow, bool used, bool editable)
        {
            if (!arrow)
                return;

            if (!editable)
            {
                arrow.color = _disabledArrowColor;
                return;
            }

            if (used)
            {
                arrow.color = _usedArrowColor;
                return;
            }

            arrow.color = _selected ? _selectedArrowColor : _normalArrowColor;
        }

        private float WrapValue(float value)
        {
            float step = Mathf.Max(0.01f, _step);
            int count = Mathf.FloorToInt((_maxValue - _minValue) / step) + 1;

            if (count <= 0)
                return _minValue;

            int index = Mathf.RoundToInt((value - _minValue) / step);
            index %= count;

            if (index < 0)
                index += count;

            return _minValue + index * step;
        }
    }
}