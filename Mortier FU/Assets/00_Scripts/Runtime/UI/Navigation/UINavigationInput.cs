using UnityEngine;

namespace MortierFu
{
    public enum UINavigationAxis
    {
        None,
        Horizontal,
        Vertical
    }

    public readonly struct UINavigationInput
    {
        public readonly UINavigationAxis Axis;
        public readonly int Direction;

        public UINavigationInput(UINavigationAxis axis, int direction)
        {
            Axis = axis;
            Direction = direction;
        }
    }

    public sealed class UINavigationRepeater
    {
        private readonly float _pressThreshold;
        private readonly float _releaseThreshold;
        private readonly float _axisDominanceMargin;
        private readonly float _initialRepeatDelay;
        private readonly float _repeatCooldown;

        private UINavigationAxis _lockedAxis;
        private int _lockedDirection;
        private float _nextNavigateTime;

        public UINavigationRepeater(float pressThreshold, float releaseThreshold, float axisDominanceMargin, float initialRepeatDelay, float repeatCooldown)
        {
            _pressThreshold = Mathf.Max(0.01f, pressThreshold);
            _releaseThreshold = Mathf.Clamp(releaseThreshold, 0.01f, _pressThreshold);
            _axisDominanceMargin = Mathf.Max(0f, axisDominanceMargin);
            _initialRepeatDelay = Mathf.Max(0.01f, initialRepeatDelay);
            _repeatCooldown = Mathf.Max(0.01f, repeatCooldown);
        }

        public bool TryGetNavigation(Vector2 input, out UINavigationInput navigation)
        {
            navigation = default;

            if (!IsReleased(input))
                return _lockedAxis == UINavigationAxis.None ? TryStartNavigation(input, out navigation) : TryRepeatNavigation(input, out navigation);
          
            Reset();
            return false;
        }

        public void Reset()
        {
            _lockedAxis = UINavigationAxis.None;
            _lockedDirection = 0;
            _nextNavigateTime = 0f;
        }

        private bool TryStartNavigation(Vector2 input, out UINavigationInput navigation)
        {
            navigation = default;

            if (!TryResolveDominantAxis(input, out UINavigationAxis axis, out int direction))
                return false;

            _lockedAxis = axis;
            _lockedDirection = direction;
            _nextNavigateTime = Time.unscaledTime + _initialRepeatDelay;

            navigation = new UINavigationInput(axis, direction);
            return true;
        }

        private bool TryRepeatNavigation(Vector2 input, out UINavigationInput navigation)
        {
            navigation = default;

            int currentDirection = GetDirectionOnLockedAxis(input);

            if (currentDirection == 0)
                return false;

            if (currentDirection != _lockedDirection)
                return false;

            if (Time.unscaledTime < _nextNavigateTime)
                return false;

            _nextNavigateTime = Time.unscaledTime + _repeatCooldown;

            navigation = new UINavigationInput(_lockedAxis, _lockedDirection);
            return true;
        }

        private bool TryResolveDominantAxis(Vector2 input, out UINavigationAxis axis, out int direction)
        {
            axis = UINavigationAxis.None;
            direction = 0;

            float absX = Mathf.Abs(input.x);
            float absY = Mathf.Abs(input.y);

            bool horizontalStrong = absX >= _pressThreshold && absX > absY + _axisDominanceMargin;
            bool verticalStrong = absY >= _pressThreshold && absY > absX + _axisDominanceMargin;

            if (horizontalStrong)
            {
                axis = UINavigationAxis.Horizontal;
                direction = input.x > 0f ? 1 : -1;
                return true;
            }

            if (!verticalStrong) return false;
            
            axis = UINavigationAxis.Vertical;
            direction = input.y > 0f ? 1 : -1;
            return true;
        }

        private int GetDirectionOnLockedAxis(Vector2 input)
        {
            float value = _lockedAxis == UINavigationAxis.Horizontal ? input.x : input.y;

            if (Mathf.Abs(value) < _releaseThreshold)
                return 0;

            return value > 0f ? 1 : -1;
        }

        private bool IsReleased(Vector2 input) => Mathf.Abs(input.x) < _releaseThreshold && Mathf.Abs(input.y) < _releaseThreshold;
    }
}