using UnityEngine;

namespace MortierFu
{
    public sealed class StaticBullyMazeRaceModeRuntime : RaceModeRuntimeBase
    {
        private SO_StaticBullyMazeRaceModeDefinition MazeDefinition => Definition as SO_StaticBullyMazeRaceModeDefinition;

        public override Transform ResolveSpawnPoint(PlayerTeam team, PlayerManager player, int racerIndex, Transform fallback)
        {
            if (!Reporter)
                return fallback;

            if (IsBully(player))
                return Reporter.BullySpawnPoint ? Reporter.BullySpawnPoint : fallback;

            Transform racerSpawn = Reporter.GetRacerSpawnPoint(racerIndex);
            return racerSpawn ? racerSpawn : fallback;
        }

        public override RaceAugmentLayout BuildAugmentLayout(int augmentCount)
        {
            if (!Reporter || augmentCount <= 0)
                return base.BuildAugmentLayout(augmentCount);

            Vector3[] points = new Vector3[augmentCount];

            if (MazeDefinition != null && MazeDefinition.UseFixedAugmentPoints)
            {
                if (Reporter.TryPopulateFixedAugmentPoints(points))
                    return new RaceAugmentLayout(Reporter.AugmentPivot, points, parentPointsToPivot: false, useRotatorPrediction: false);
            }

            PlayerCharacter bullyCharacter = BullyCharacter;

            if (!bullyCharacter)
                return base.BuildAugmentLayout(augmentCount);

            Reporter.TryPopulateCircleAround(bullyCharacter.transform.position, points);

            return new RaceAugmentLayout(Reporter.AugmentPivot, points, parentPointsToPivot: false, useRotatorPrediction: false);
        }
    }
}