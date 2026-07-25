using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MortierFu
{
    public sealed class UIEditableSlider : Slider, ISubmitHandler, ICancelHandler
    {
        [Header("Edit Mode")]
        [SerializeField, Min(0.001f)] private float _step = 0.05f;
        [SerializeField] private bool _exitEditOnVerticalMove = true;

        private UISliderSelectionVisual _selectionVisual;
        private bool _isEditing;

        protected override void Awake()
        {
            base.Awake();
            _selectionVisual = GetComponent<UISliderSelectionVisual>();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SetEditing(false);
        }

        public override void OnMove(AxisEventData eventData)
        {
            if (!_isEditing)
            {
                NavigateExplicit(eventData);
                return;
            }

            if (eventData.moveDir is MoveDirection.Left or MoveDirection.Right)
            {
                float inputDirection = eventData.moveDir == MoveDirection.Right ? 1f : -1f;
                value = Mathf.Clamp(value + inputDirection * ResolveStep(), minValue, maxValue);

                eventData.Use();
                return;
            }

            if (_exitEditOnVerticalMove)
            {
                SetEditing(false);
                NavigateExplicit(eventData);
                return;
            }

            eventData.Use();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            SetEditing(!_isEditing);
            eventData.Use();
        }

        public void OnCancel(BaseEventData eventData)
        {
            if (!_isEditing)
                return;

            SetEditing(false);
            eventData.Use();
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            SetEditing(false);
            base.OnDeselect(eventData);
        }

        private void SetEditing(bool editing)
        {
            if (_isEditing == editing)
                return;

            _isEditing = editing;
            _selectionVisual?.SetEditing(_isEditing);
        }

        private float ResolveStep() => wholeNumbers ? 1f : Mathf.Max(0.001f, _step);

        private void NavigateExplicit(AxisEventData eventData)
        {
            Selectable target = eventData.moveDir switch
            {
                MoveDirection.Left => navigation.selectOnLeft,
                MoveDirection.Right => navigation.selectOnRight,
                MoveDirection.Up => navigation.selectOnUp,
                MoveDirection.Down => navigation.selectOnDown,
                _ => null
            };

            if (!target || !target.gameObject.activeInHierarchy || !target.IsInteractable())
            {
                eventData.Use();
                return;
            }

            target.Select();
            eventData.Use();
        }
    }
}