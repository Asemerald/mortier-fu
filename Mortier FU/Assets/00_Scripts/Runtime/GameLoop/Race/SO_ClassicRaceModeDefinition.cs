using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_RaceMode_Classic", menuName = "Mortier Fu/Race Modes/Classic")]
    public sealed class SO_ClassicRaceModeDefinition : SO_RaceModeDefinition
    {
        private void Reset()
        {
            UsePreviousRoundWinnerAsBully = true;
            BullyContext = PlayerControlContext.AugmentRaceBullyClassic;
            RacerContext = PlayerControlContext.AugmentRace;
        }

        public override RaceModeRuntimeBase CreateRuntime() => new ClassicRaceModeRuntime();
    }
}