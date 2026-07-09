using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MortierFu.Shared;

namespace MortierFu
{
    public sealed class RoundController : IDisposable
    {
        private readonly IReadOnlyList<PlayerTeam> _teams;
        private readonly List<PlayerCharacter> _alivePlayers;

        private readonly EventBinding<EventPlayerDeath> _playerDeathBinding;

        private int _currentRank;
        private bool _isListeningToDeaths;

        public ReadOnlyCollection<PlayerCharacter> AlivePlayers { get; }

        public bool OneTeamStanding { get; private set; }

        public PlayerTeam WinningTeam { get; private set; }

        public event Action<PlayerCharacter> OnPlayerDied;

        public event Action<PlayerManager, PlayerManager> OnPlayerKilled;

        public RoundController(IReadOnlyList<PlayerTeam> teams, List<PlayerCharacter> alivePlayers)
        {
            _teams = teams ?? throw new ArgumentNullException(nameof(teams));
            _alivePlayers = alivePlayers ?? throw new ArgumentNullException(nameof(alivePlayers));

            AlivePlayers = _alivePlayers.AsReadOnly();
            _playerDeathBinding = new EventBinding<EventPlayerDeath>(OnPlayerDeath);
        }

        public void BeginRound()
        {
            _alivePlayers.Clear();

            _currentRank = _teams.Count;
            OneTeamStanding = false;
            WinningTeam = null;

            foreach (var team in _teams)
            {
                team.Rank = -1;

                foreach (var member in team.Members)
                {
                    member.Metrics.ResetRoundMetrics();

                    if (member.Character != null)
                    {
                        _alivePlayers.Add(member.Character);
                    }
                }
            }

            StartListeningToDeaths();
        }

        public void EndRound()
        {
            StopListeningToDeaths();
            _alivePlayers.Clear();
        }

        public void Dispose()
        {
            StopListeningToDeaths();

            OnPlayerDied = null;
            OnPlayerKilled = null;
        }

        private void StartListeningToDeaths()
        {
            if (_isListeningToDeaths)
                return;

            EventBus<EventPlayerDeath>.Register(_playerDeathBinding);
            _isListeningToDeaths = true;
        }

        private void StopListeningToDeaths()
        {
            if (!_isListeningToDeaths)
                return;

            EventBus<EventPlayerDeath>.Deregister(_playerDeathBinding);
            _isListeningToDeaths = false;
        }

        private void OnPlayerDeath(EventPlayerDeath evt)
        {
            if (evt.Character == null || evt.Character.Owner == null)
                return;

            if (!_alivePlayers.Remove(evt.Character))
                return;

            PlayerManager victimPlayer = evt.Character.Owner;

            victimPlayer.Metrics.TotalDeaths++;

            OnPlayerDied?.Invoke(evt.Character);

            if (evt.Context.Killer != null)
                HandlePlayerKill(evt);

            PlayerTeam victimTeam = FindTeamOf(victimPlayer);

            if (victimTeam == null)
            {
                Logs.LogError("[RoundController] Victim's team not found.");
                return;
            }

            TryAssignEliminatedTeamRank(victimTeam);
            TryResolveWinningTeam();
        }
        
        private bool IsAliveInCurrentRound(PlayerManager member)
        {
            return member && member.Character && _alivePlayers.Contains(member.Character);
        }

        private void HandlePlayerKill(EventPlayerDeath evt)
        {
            var killerPlayer = evt.Context.Killer.Owner;
            var victimPlayer = evt.Character.Owner;

            if (killerPlayer == null || victimPlayer == null)
                return;

            if (killerPlayer != victimPlayer)
            {
                killerPlayer.Metrics.RoundKills.Add(evt.Context.DeathCause);
            }

            OnPlayerKilled?.Invoke(killerPlayer, victimPlayer);
        }

        private PlayerTeam FindTeamOf(PlayerManager player)
        {
            return _teams.FirstOrDefault(team => team.Members.Contains(player));
        }

        private void TryAssignEliminatedTeamRank(PlayerTeam victimTeam)
        {
            bool isTeamEliminated = victimTeam.Members.All(member => !IsAliveInCurrentRound(member));

            if (!isTeamEliminated)
                return;

            victimTeam.Rank = _currentRank;
            _currentRank--;

            Logs.Log($"[RoundController] Team {victimTeam.Index} eliminated, assigned rank #{victimTeam.Rank}.");
        }

        private void TryResolveWinningTeam()
        {
            PlayerTeam aliveTeam = null;

            foreach (PlayerTeam team in _teams)
            {
                bool hasAliveMember = team.Members.Any(IsAliveInCurrentRound);

                if (!hasAliveMember)
                    continue;

                if (aliveTeam != null) return;
                aliveTeam = team;
            }

            if (aliveTeam == null)
                return;

            aliveTeam.Rank = 1;
            WinningTeam = aliveTeam;
            OneTeamStanding = true;
        }
    }
}