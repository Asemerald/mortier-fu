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
    }
}