using System;
using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class PlayerTeamSetupController
    {
        public List<PlayerTeam> CreateFreeForAllTeams(IReadOnlyList<PlayerManager> players)
        {
            if (players == null)
                throw new ArgumentNullException(nameof(players));

            var teams = new List<PlayerTeam>(players.Count);

            for (int i = 0; i < players.Count; i++)
            {
                PlayerManager player = players[i];

                if (player == null)
                {
                    Logs.LogWarning($"[PlayerTeamSetupController] Player at index {i} is null. Skipping.");
                    continue;
                }

                SpawnPlayerInTemporaryStartPosition(player, i);

                var team = new PlayerTeam(
                    index: teams.Count,
                    player
                );

                teams.Add(team);
            }

            Logs.Log($"[PlayerTeamSetupController] Created {teams.Count} free-for-all teams.");

            return teams;
        }

        private static void SpawnPlayerInTemporaryStartPosition(PlayerManager player, int playerIndex)
        {
            Vector3 position = new Vector3(playerIndex, 5f, playerIndex) * 2f;
            Quaternion rotation = player.transform.rotation;

            player.SpawnInGame(position, rotation);
        }
    }
}