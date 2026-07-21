using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MortierFu
{
    public sealed class UIMatchRulesetSelectorItem : UIMatchSettingsItemBase
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

        private bool _selected;
        private int _lastDirection;

        public override bool HandleHorizontal(int direction)
        {
            if (direction == 0 || Data == null)
                return false;

            Data.SelectRulesetByOffset(direction, PlayerCount);

            _lastDirection = direction;
            Refresh();

            return true;
        }

        public override bool HandleSubmit()
        {
            return HandleHorizontal(1);
        }

        protected override void OnSelectionChanged(bool selected)
        {
            _selected = selected;

            if (!selected)
                _lastDirection = 0;

            UpdateArrows();
        }

        public override void Refresh()
        {
            SO_MatchRuleset ruleset = Data ? Data.SelectedRuleset : null;

            if (_nameText)
                _nameText.text = ruleset ? ruleset.DisplayName : "CUSTOM GAME";

            if (_subtitleText)
                _subtitleText.text = ruleset ? ruleset.Subtitle : string.Empty;

            if (_descriptionText)
                _descriptionText.text = ruleset ? ruleset.Description : string.Empty;

            SetReadOnlyVisual(editable: true);
            UpdateArrows();
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

            arrow.color = _selected ? _selectedArrowColor : _normalArrowColor;
        }
    }
}