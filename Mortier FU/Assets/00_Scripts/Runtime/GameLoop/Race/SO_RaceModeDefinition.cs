using UnityEngine;

namespace MortierFu
{
    public abstract class SO_RaceModeDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string DisplayName;

        [Header("Bully")]
        public bool UsePreviousRoundWinnerAsBully = true;
        public bool BullyCanPickAugment = false;
        public PlayerControlContext BullyContext = PlayerControlContext.AugmentRaceBully;
        public PlayerControlContext RacerContext = PlayerControlContext.AugmentRace;

        [Header("Duration")]
        public bool OverrideRaceDuration;
        [Min(0.1f)] public float RaceDuration = 20f;

        [Header("Bully Size")]
        public bool UseGameFlowBullySize = true;
        public bool OverrideBullySize;
        [Min(0.1f)] public float BullyTargetSize = 3.5f;

        public abstract RaceModeRuntimeBase CreateRuntime();

        public float ResolveRaceDuration(float defaultDuration) => OverrideRaceDuration ? Mathf.Max(0.1f, RaceDuration) : Mathf.Max(0.1f, defaultDuration);

        public bool ShouldApplyBullySize(SO_GameFlowSettings flowSettings)
        {
            if (!UsePreviousRoundWinnerAsBully)
                return false;

            if (OverrideBullySize)
                return true;

            return UseGameFlowBullySize && flowSettings && flowSettings.EnablePreviousRoundWinnerRaceGiant;
        }

        public float ResolveBullyTargetSize(SO_GameFlowSettings flowSettings)
        {
            if (OverrideBullySize)
                return Mathf.Max(0.1f, BullyTargetSize);

            if (flowSettings)
                return Mathf.Max(0.1f, flowSettings.PreviousRoundWinnerRaceTargetSize);

            return Mathf.Max(0.1f, BullyTargetSize);
        }
    }
}