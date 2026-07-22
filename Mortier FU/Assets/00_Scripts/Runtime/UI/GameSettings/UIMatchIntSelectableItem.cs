using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MortierFu
{
    public sealed class UIMatchIntSelectableItem : UIMatchSelectableItemBase
    {
        [Header("Setting")]
        [SerializeField] private MatchSettingId _settingId = MatchSettingId.ScoreToWin;

        [Header("Value")]
        [SerializeField] private int _minValue = 500;
        [SerializeField] private int _maxValue = 3000;
        [SerializeField] private int _step = 100;
        [SerializeField] private bool _wrapValue;

        [Header("Format")]
        [SerializeField] private string _prefix = "";
        [SerializeField] private string _suffix = "";

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
            int value = Data ? Data.GetInt(_settingId) : 0;
            bool editable = Data && Data.IsSettingEditable(_settingId);

            if (_valueText)
                _valueText.text = $"{_prefix}{value}{_suffix}";

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

            int currentValue = Data.GetInt(_settingId);
            int step = Mathf.Max(1, _step);
            int nextValue = currentValue + direction * step;

            nextValue = _wrapValue ? WrapValue(nextValue) : Mathf.Clamp(nextValue, _minValue, _maxValue);

            Data.SetInt(_settingId, nextValue);

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

        private int WrapValue(int value)
        {
            int step = Mathf.Max(1, _step);
            int count = ((_maxValue - _minValue) / step) + 1;

            if (count <= 0)
                return _minValue;

            int index = Mathf.RoundToInt((value - _minValue) / (float)step);
            index %= count;

            if (index < 0)
                index += count;

            return _minValue + index * step;
        }
    }
}