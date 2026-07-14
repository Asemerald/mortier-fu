using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_RaceMode_StaticBullyMaze", menuName = "Mortier Fu/Race Modes/Static Bully Maze")]
    public sealed class SO_StaticBullyMazeRaceModeDefinition : SO_RaceModeDefinition
    {
        private void Reset()
        {
            UsePreviousRoundWinnerAsBully = true;
            BullyContext = PlayerControlContext.AugmentRaceBullyShootOnly;
            RacerContext = PlayerControlContext.AugmentRace;
        }

        public override RaceModeRuntimeBase CreateRuntime() => new StaticBullyMazeRaceModeRuntime();
    }
}