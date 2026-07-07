using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MortierFu
{
    [Serializable]
    public sealed class IntValueChangedEvent : UnityEvent<int>
    { }

    public sealed class UIIntStepperItem : UINavigationItem
    {
        [Header("Value")]
        [SerializeField] private int _minValue = 1;
        [SerializeField] private int _maxValue = 10;
        [SerializeField] private int _step = 1;
        [SerializeField] private int _currentValue = 3;
        [SerializeField] private bool _wrapValue = false;

        [Header("Visuals")]
        [SerializeField] private TMP_Text _valueText;
        [SerializeField] private Graphic _leftArrow;
        [SerializeField] private Graphic _rightArrow;
        [SerializeField] private Color _normalArrowColor = Color.white;
        [SerializeField] private Color _selectedArrowColor = Color.yellow;
        [SerializeField] private Color _usedArrowColor = Color.green;

        [Header("Events")]
        [SerializeField] private IntValueChangedEvent _onValueChanged;

        private bool _selected;
        private int _lastDirection;

        public event Action<int> OnValueChanged;

        private void Awake()
        {
            ClampSettings();
            SetValue(_currentValue, notify: false);
            UpdateVisuals();
        }

        public override bool HandleHorizontal(int direction)
        {
            if (direction == 0)
                return false;

            int nextValue = _currentValue + direction * Mathf.Max(1, _step);

            SetValue(nextValue, notify: true);

            _lastDirection = direction;
            UpdateVisuals();

            return true;
        }

        public void SetValue(int value, bool notify = true)
        {
            ClampSettings();

            int newValue = _wrapValue ? WrapValue(value) : Mathf.Clamp(value, _minValue, _maxValue);

            if (_currentValue == newValue)
            {
                UpdateVisuals();
                return;
            }

            _currentValue = newValue;

            UpdateVisuals();

            if (!notify)
                return;

            OnValueChanged?.Invoke(_currentValue);
            _onValueChanged?.Invoke(_currentValue);
        }

        protected override void OnSelectionChanged(bool selected)
        {
            _selected = selected;

            if (!selected)
                _lastDirection = 0;

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_valueText)
                _valueText.text = _currentValue.ToString();

            UpdateArrow(_leftArrow, _lastDirection < 0);
            UpdateArrow(_rightArrow, _lastDirection > 0);
        }

        private void UpdateArrow(Graphic arrow, bool used)
        {
            if (!arrow)
                return;

            if (used)
            {
                arrow.color = _usedArrowColor;
                return;
            }

            arrow.color = _selected ? _selectedArrowColor : _normalArrowColor;
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

        private void ClampSettings()
        {
            if (_maxValue < _minValue)
                _maxValue = _minValue;

            if (_step <= 0)
                _step = 1;
        }
    }
}