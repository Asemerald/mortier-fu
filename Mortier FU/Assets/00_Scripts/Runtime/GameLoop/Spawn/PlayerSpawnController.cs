using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MortierFu
{
    public sealed class PlayerSpawnController
    {
        private readonly IReadOnlyList<PlayerTeam> _teams;
        private readonly LevelSystem _levelSystem;

        public PlayerSpawnController(
            IReadOnlyList<PlayerTeam> teams,
            LevelSystem levelSystem)
        {
            _teams = teams ?? throw new ArgumentNullException(nameof(teams));
            _levelSystem = levelSystem ?? throw new ArgumentNullException(nameof(levelSystem));
        }

        public void ResetPlayers()
        {
            foreach (var team in _teams)
            {
                foreach (var member in team.Members)
                {
                    member.Character?.Reset();
                }
            }
        }

        public void SetPlayerGravity(bool enabled)
        {
            foreach (var team in _teams)
            {
                foreach (var member in team.Members)
                {
                    if (member.Character == null)
                        continue;

                    if (member.Character.Controller == null)
                        continue;

                    if (member.Character.Controller.rigidbody == null)
                        continue;

                    member.Character.Controller.rigidbody.useGravity = enabled;
                }
            }
        }

        public void SpawnPlayers(int roundIndex)
        {
            bool opposite = roundIndex % 2 == 0;
            int spawnIndex = opposite
                ? _teams.Sum(team => team.Members.Count) - 1
                : 0;

            foreach (var team in _teams.OrderByDescending(team => team.Rank))
            {
                foreach (var member in team.Members)
                {
                    Transform spawnPoint = GetSpawnPointFor(team, spawnIndex);

                    member.SpawnInGame(spawnPoint.position, spawnPoint.rotation);

                    if (opposite)
                        spawnIndex--;
                    else
                        spawnIndex++;
                }
            }
        }

        public void SpawnWinnerTeam(PlayerTeam winnerTeam)
        {
            if (winnerTeam == null)
                return;

            Transform spawnPoint = _levelSystem.GetRoundWinnerSpawnPoint();

            foreach (var member in winnerTeam.Members)
            {
                member.SpawnInGame(spawnPoint.position, spawnPoint.rotation);
            }
        }

        private Transform GetSpawnPointFor(PlayerTeam team, int spawnIndex)
        {
            if (_levelSystem.IsRaceMap())
            {
                return team.Rank == 1
                    ? _levelSystem.GetWinnerSpawnPoint()
                    : _levelSystem.GetSpawnPoint(spawnIndex);
            }

            return _levelSystem.GetSpawnPoint(spawnIndex);
        }
    }
}