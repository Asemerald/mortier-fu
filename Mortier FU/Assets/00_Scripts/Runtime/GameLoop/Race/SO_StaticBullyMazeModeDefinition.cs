using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(
        fileName = "DA_RaceMode_StaticBullyMaze",
        menuName = "Mortier Fu/Race Modes/Static Bully Maze")]
    public sealed class SO_StaticBullyMazeRaceModeDefinition : SO_RaceModeDefinition
    {
        [Header("Augments")]
        public bool UseFixedAugmentPoints;

        private void Reset()
        {
            DisplayName = "Static Bully Maze";
            UsePreviousRoundWinnerAsBully = true;
            BullyCanPickAugment = false;
            BullyContext = PlayerControlContext.AugmentRaceBullyShootOnly;
            RacerContext = PlayerControlContext.AugmentRace;
            UseGameFlowBullySize = true;
        }

        public override RaceModeRuntimeBase CreateRuntime() => new StaticBullyMazeRaceModeRuntime();
    }
}