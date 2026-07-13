using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(
        fileName = "DA_RaceMode_Pinhata",
        menuName = "Mortier Fu/Race Modes/Pinhata"
    )]
    public sealed class SO_PinhataRaceModeDefinition : SO_RaceModeDefinition
    {
        [Header("Drop")]
        [Min(0.1f)] public float DropRadius = 2.5f;
        [Min(0f)] public float DropHeight = 1.2f;
        [Min(0.05f)] public float DropDuration = 0.35f;
        [Min(0f)] public float HitCooldown = 0.25f;
        
        [Header("Pinhata Pickup Positions")]
        public bool OverrideInsideBullyWorldY = false;
        public float InsideBullyWorldY = 1.2f;

        public bool OverrideDropWorldY = false;
        public float DropWorldY = 0.5f;

        private void Reset()
        {
            UsePreviousRoundWinnerAsBully = true;
            BullyCanPickAugment = false;
            BullyContext = PlayerControlContext.AugmentRaceBullyClassic;
            RacerContext = PlayerControlContext.AugmentRace;
        }

        public override RaceModeRuntimeBase CreateRuntime() => new PinhataRaceModeRuntime();
    }
}