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
        [Min(0.1f)] public float DropDuration = 0.35f;
        [Min(0f)] public float HitCooldown = 0.25f;
        [Min(0f)] public float InhalePickupDuration = 0.45f;
        
        [Header("Pinhata Pickup Positions")]
        public bool OverrideDropWorldY = false;
        public float DropWorldY = 1.93f;
        
        [Header("Drop Safety")]
        public LayerMask DropBlockingMask;
        [Min(0.05f)] public float DropClearanceRadius = 0.6f;
        [Min(0f)] public float DropProbeHeight = 0.6f;

        private void Reset()
        {
            UsePreviousRoundWinnerAsBully = true;
            BullyContext = PlayerControlContext.AugmentRaceBullyClassic;
            RacerContext = PlayerControlContext.AugmentRace;
        }

        public override RaceModeRuntimeBase CreateRuntime() => new PinhataRaceModeRuntime();
    }
}