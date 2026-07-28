using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using PrimeTween;

namespace MortierFu
{
    [Serializable]
    public sealed class IntValueChangedEvent : UnityEvent<int>
    { }

    public sealed class UIIntStepperItem : UINavigationItem
    {
        #region Variables

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
        [SerializeField] private Color _usedArrowColor = Color.green;

        [SerializeField] private float _usedFlashDuration = 0.3f;
        [SerializeField] private Ease _usedFlashEase = Ease.OutQuad;

        [Header("Events")]
        [SerializeField] private IntValueChangedEvent _onValueChanged;

        private bool _selected;

        private Tween _leftArrowTween;
        private Tween _rightArrowTween;

        public event Action<int> OnValueChanged;

        #endregion

        #region Unity LifeCycle

        private void Awake()
        {
            ClampSettings();
            SetValue(_currentValue, notify: false);
            UpdateVisuals();
        }

        #endregion

        #region Core

        public override bool HandleHorizontal(int direction)
        {
            if (direction == 0)
                return false;

            int previousValue = _currentValue;
            int nextValue = _currentValue + direction * Mathf.Max(1, _step);

            SetValue(nextValue, notify: true);

            if (_currentValue != previousValue)
            {
                if (direction < 0)
                    FlashArrow(_leftArrow, ref _leftArrowTween);
                else
                    FlashArrow(_rightArrow, ref _rightArrowTween);
            }

            return true;
        }

        private void FlashArrow(Graphic arrow, ref Tween tween)
        {
            if (!arrow)
                return;

            tween.Stop();
            arrow.color = _usedArrowColor;
            tween = Tween.Color(arrow, _selected ? _selectedArrowColor : _normalArrowColor, _usedFlashDuration, _usedFlashEase);
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
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_valueText)
                _valueText.text = _currentValue.ToString();

            UpdateArrowColor(_leftArrow);
            UpdateArrowColor(_rightArrow);
        }

        private void UpdateArrowColor(Graphic arrow)
        {
            if (!arrow)
                return;

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

        #endregion

        #region Clean & Setup

        public void ConfigureRange(int minValue, int maxValue, int step, bool wrapValue)
        {
            _minValue = minValue;
            _maxValue = Mathf.Max(minValue, maxValue);
            _step = Mathf.Max(1, step);
            _wrapValue = wrapValue;

            SetValue(_currentValue, notify: false);
        }

        private void ClampSettings()
        {
            if (_maxValue < _minValue)
                _maxValue = _minValue;

            if (_step <= 0)
                _step = 1;
        }

        public void ResetUsageFeedback()
        {
            _leftArrowTween.Stop();
            _rightArrowTween.Stop();
            UpdateVisuals();
        }

        #endregion
    }
}