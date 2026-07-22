using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MortierFu
{
    public abstract class UIMatchSelectableItemBase : Selectable, ISubmitHandler, ICancelHandler
    {
        [Header("Selection Visual")]
        [SerializeField] private Graphic _selectionGraphic;
        [SerializeField] private Color _normalColor = new(1f, 1f, 1f, 0f);
        [SerializeField] private Color _selectedColor = Color.yellow;

        [Header("Read Only")]
        [SerializeField] private CanvasGroup _contentGroup;
        [SerializeField, Range(0f, 1f)] private float _readOnlyAlpha = 0.45f;

        protected LobbySettingsPanel Panel { get; private set; }
        protected LobbyMatchSettingsData Data { get; private set; }
        protected int PlayerCount { get; private set; }

        public bool CanReceiveSelection => isActiveAndEnabled && gameObject.activeInHierarchy && IsInteractable();

        protected override void OnEnable()
        {
            base.OnEnable();
            SetSelectedVisual(false);
        }

        public void Bind(LobbySettingsPanel panel, LobbyMatchSettingsData data, int playerCount)
        {
            Panel = panel;
            Data = data;
            PlayerCount = Mathf.Max(1, playerCount);

            Refresh();
        }

        public abstract void Refresh();

        public override void OnMove(AxisEventData eventData)
        {
            if (eventData.moveDir == MoveDirection.Up)
            {
                Panel?.SelectRelativeTo(this, -1);
                eventData.Use();
                return;
            }

            if (eventData.moveDir == MoveDirection.Down)
            {
                Panel?.SelectRelativeTo(this, 1);
                eventData.Use();
                return;
            }

            base.OnMove(eventData);
        }

        public virtual void OnSubmit(BaseEventData eventData)
        { }

        public virtual void OnCancel(BaseEventData eventData)
        {
            eventData.Use();
            Panel?.CloseFromUI();
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);

            if (!CanReceiveSelection)
            {
                Panel?.ValidateCurrentSelection();
                return;
            }

            SetSelectedVisual(true);
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            SetSelectedVisual(false);
        }

        protected void SetItemInteractable(bool itemInteractable)
        {
            interactable = itemInteractable;
            SetReadOnlyVisual(itemInteractable);

            EventSystem eventSystem = EventSystem.current;

            if (!itemInteractable && eventSystem && eventSystem.currentSelectedGameObject == gameObject)
                Panel?.ValidateCurrentSelection();
        }

        protected void SetReadOnlyVisual(bool editable)
        {
            if (_contentGroup)
                _contentGroup.alpha = editable ? 1f : _readOnlyAlpha;
        }

        protected static bool TryGetHorizontalMove(AxisEventData eventData, out int direction)
        {
            direction = 0;

            if (eventData.moveDir == MoveDirection.Left)
            {
                direction = -1;
                return true;
            }

            if (eventData.moveDir == MoveDirection.Right)
            {
                direction = 1;
                return true;
            }

            return false;
        }

        private void SetSelectedVisual(bool selected)
        {
            if (_selectionGraphic)
                _selectionGraphic.color = selected ? _selectedColor : _normalColor;
        }
    }
}