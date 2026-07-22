using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MortierFu
{
    public sealed class UIMatchFloatSelectableItem : UIMatchSelectableItemBase
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

        private bool _isSelected;
        private int _lastDirection;

        public override void OnMove(AxisEventData eventData)
        {
            if (TryGetHorizontalMove(eventData, out int direction))
            {
                ChangeValue(direction);
                eventData.Use();
                return;
            }

            base.OnMove(eventData);
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
            _lastDirection = 0;
            base.OnDeselect(eventData);
            Refresh();
        }

        public override void Refresh()
        {
            float value = Data ? Data.GetFloat(_settingId) : 0f;
            bool editable = Data && Data.IsSettingEditable(_settingId);

            if (_valueText)
                _valueText.text = $"{_prefix}{value.ToString(_format)}{_suffix}";

            SetItemInteractable(editable);
            UpdateArrow(_leftArrow, _lastDirection < 0, editable);
            UpdateArrow(_rightArrow, _lastDirection > 0, editable);
        }

        private void ChangeValue(int direction)
        {
            if (Data == null || direction == 0)
                return;

            bool editable = Data.IsSettingEditable(_settingId);

            if (!editable)
            {
                _lastDirection = 0;
                Refresh();
                return;
            }

            float step = Mathf.Max(0.01f, _step);
            float currentValue = Data.GetFloat(_settingId);
            float nextValue = currentValue + direction * step;

            nextValue = _wrapValue ? WrapValue(nextValue) : Mathf.Clamp(nextValue, _minValue, _maxValue);
            nextValue = Quantize(nextValue);

            Data.SetFloat(_settingId, nextValue);

            _lastDirection = direction;
            Refresh();
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

            arrow.color = _isSelected ? _selectedArrowColor : _normalArrowColor;
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

        private float Quantize(float value)
        {
            float step = Mathf.Max(0.01f, _step);
            float stepsFromMin = Mathf.Round((value - _minValue) / step);

            return _minValue + stepsFromMin * step;
        }
    }
}