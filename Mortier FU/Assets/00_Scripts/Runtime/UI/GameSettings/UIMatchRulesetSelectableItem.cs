using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MortierFu
{
    public sealed class UIMatchRulesetSelectableItem : UIMatchSelectableItemBase
    {
        [Header("Text")]
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _subtitleText;
        [SerializeField] private TMP_Text _descriptionText;

        [Header("Arrows")]
        [SerializeField] private Graphic _leftArrow;
        [SerializeField] private Graphic _rightArrow;
        [SerializeField] private Color _normalArrowColor = Color.white;
        [SerializeField] private Color _selectedArrowColor = Color.yellow;
        [SerializeField] private Color _usedArrowColor = Color.green;

        private bool _isSelected;
        private int _lastDirection;

        public override void OnMove(AxisEventData eventData)
        {
            if (TryGetHorizontalMove(eventData, out int direction))
            {
                ChangeRuleset(direction);
                eventData.Use();
                return;
            }

            base.OnMove(eventData);
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            ChangeRuleset(1);
            eventData.Use();
        }

        public override void OnSelect(BaseEventData eventData)
        {
            _isSelected = true;
            base.OnSelect(eventData);
            UpdateArrows();
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            _isSelected = false;
            _lastDirection = 0;
            base.OnDeselect(eventData);
            UpdateArrows();
        }

        public override void Refresh()
        {
            SetItemInteractable(true);

            SO_MatchRuleset ruleset = Data ? Data.SelectedRuleset : null;

            if (_nameText)
                _nameText.text = ruleset ? ruleset.DisplayName : "CUSTOM GAME";

            if (_subtitleText)
                _subtitleText.text = ruleset ? ruleset.Subtitle : string.Empty;

            if (_descriptionText)
                _descriptionText.text = ruleset ? ruleset.Description : string.Empty;

            UpdateArrows();
        }

        private void ChangeRuleset(int direction)
        {
            if (Data == null || direction == 0)
                return;

            Data.SelectRulesetByOffset(direction, PlayerCount);

            _lastDirection = direction;
            Refresh();
        }

        private void UpdateArrows()
        {
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

            arrow.color = _isSelected ? _selectedArrowColor : _normalArrowColor;
        }
    }
}