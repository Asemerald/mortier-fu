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
        private readonly float _threshold;
        private readonly float _cooldown;

        private UINavigationAxis _previousAxis;
        private int _previousDirection;
        private float _lastNavigateTime;

        public UINavigationRepeater(float threshold, float cooldown)
        {
            _threshold = Mathf.Max(0.01f, threshold);
            _cooldown = Mathf.Max(0.01f, cooldown);
        }

        public bool TryGetNavigation(Vector2 input, out UINavigationInput navigation)
        {
            navigation = default;

            if (!TryResolveAxis(input, out UINavigationAxis axis, out int direction))
            {
                Reset();
                return false;
            }

            bool sameDirection = axis == _previousAxis && direction == _previousDirection;

            bool cooldownExpired = Time.unscaledTime - _lastNavigateTime >= _cooldown;

            if (sameDirection && !cooldownExpired)
                return false;

            _previousAxis = axis;
            _previousDirection = direction;
            _lastNavigateTime = Time.unscaledTime;

            navigation = new UINavigationInput(axis, direction);
            return true;
        }

        public void Reset()
        {
            _previousAxis = UINavigationAxis.None;
            _previousDirection = 0;
            _lastNavigateTime = 0f;
        }

        private bool TryResolveAxis(Vector2 input, out UINavigationAxis axis, out int direction)
        {
            axis = UINavigationAxis.None;
            direction = 0;

            float absX = Mathf.Abs(input.x);
            float absY = Mathf.Abs(input.y);

            if (absX < _threshold && absY < _threshold)
                return false;

            if (absX >= absY)
            {
                axis = UINavigationAxis.Horizontal;
                direction = input.x > 0f ? 1 : -1;
                return true;
            }

            axis = UINavigationAxis.Vertical;
            direction = input.y > 0f ? 1 : -1;
            return true;
        }
    }
}