using UnityEngine;

namespace MortierFu
{
    //TODO in the future make it a Bully Controller
    public sealed class PreviousRoundWinnerRaceSizeController
    {
        private readonly object _modifierSource = new PreviousRoundWinnerRaceSizeModifierSource();

        private PlayerCharacter _activeCharacter;

        public void Apply(PlayerCharacter character, float targetFinalSize, bool applyControlContext = true)
        {
            Clear();

            if (!character || !character.Stats)
                return;

            targetFinalSize = Mathf.Max(0.1f, targetFinalSize);

            float flatDelta = CalculateAvatarFlatDeltaForTargetFinalSize(character.Stats, targetFinalSize);

            if (!Mathf.Approximately(flatDelta, 0f))
                character.Stats.AvatarSize.AddModifier(new StatModifier(flatDelta, E_StatModType.Flat, _modifierSource));

            character.Aspect?.Reset();

            if (applyControlContext)
                character.SetControlContext(PlayerControlContext.AugmentRaceBullyClassic);

            _activeCharacter = character;
        }
        
        public void Clear()
        {
            if (!_activeCharacter || !_activeCharacter.Stats)
            {
                _activeCharacter = null;
                return;
            }

            _activeCharacter.Stats.AvatarSize.RemoveAllModifiersFromSource(_modifierSource);
            _activeCharacter.Aspect?.Reset();

            _activeCharacter = null;
        }

        private static float CalculateAvatarFlatDeltaForTargetFinalSize(SO_CharacterStats stats, float targetFinalSize)
        {
            var maxHealthContribution = (stats.MaxHealth.Value - stats.MaxHealth.BaseValue) * stats.MaxHealthToAvatarSizeFactor;

            var requiredAvatarStatValue = targetFinalSize - maxHealthContribution;

            return requiredAvatarStatValue - stats.AvatarSize.Value;
        }

        private sealed class PreviousRoundWinnerRaceSizeModifierSource
        { }
    }
}