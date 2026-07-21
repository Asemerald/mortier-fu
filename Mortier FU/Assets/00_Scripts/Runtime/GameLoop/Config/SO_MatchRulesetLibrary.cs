using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_MatchRulesetLibrary", menuName = "Mortier Fu/Match Settings/Ruleset Library")]
    public sealed class SO_MatchRulesetLibrary : ScriptableObject
    {
        public SO_MatchRuleset[] Rulesets;

        public int Count => Rulesets?.Length ?? 0;

        public SO_MatchRuleset Get(int index)
        {
            if (Rulesets == null || Rulesets.Length == 0)
                return null;

            index = Mathf.Clamp(index, 0, Rulesets.Length - 1);
            return Rulesets[index];
        }

        public int GetIndexOf(SO_MatchRuleset ruleset)
        {
            if (!ruleset || Rulesets == null)
                return -1;

            for (int i = 0; i < Rulesets.Length; i++)
            {
                if (Rulesets[i] == ruleset)
                    return i;
            }

            return -1;
        }

        public SO_MatchRuleset GetFirstNonCustomRuleset()
        {
            if (Rulesets == null)
                return null;

            for (int i = 0; i < Rulesets.Length; i++)
            {
                SO_MatchRuleset ruleset = Rulesets[i];

                if (ruleset && !ruleset.IsCustom)
                    return ruleset;
            }

            return Get(0);
        }

        public SO_MatchRuleset GetCustomRuleset()
        {
            if (Rulesets == null)
                return null;

            for (int i = 0; i < Rulesets.Length; i++)
            {
                SO_MatchRuleset ruleset = Rulesets[i];

                if (ruleset && ruleset.IsCustom)
                    return ruleset;
            }

            return null;
        }
    }
}