using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MortierFu.Analytics;
using MortierFu.Shared;

namespace MortierFu
{
    public sealed class ScoreController
    {
        private readonly SO_GameModeData _data;
        private readonly IReadOnlyList<PlayerTeam> _teams;
        private readonly AnalyticsSystem _analyticsSystem;

        public int ScoreToWin { get; private set; }
        public PlayerTeam GameVictor { get; private set; }

        public ScoreController(SO_GameModeData data, IReadOnlyList<PlayerTeam> teams, int scoreToWin, AnalyticsSystem analyticsSystem = null)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _teams = teams ?? throw new ArgumentNullException(nameof(teams));
            _analyticsSystem = analyticsSystem;

            ScoreToWin = scoreToWin;
            GameVictor = null;
        }

        public void SetScoreToWin(int scoreToWin) => ScoreToWin = scoreToWin;

        public bool IsGameOver(out PlayerTeam victor)
        {
            victor = GameVictor;
            return GameVictor != null;
        }

        public PlayerTeam EvaluateScores()
        {
            GameVictor = null;

            foreach (var team in _teams)
            {
                int previousScore = team.Score;

                if (team.Score >= ScoreToWin)
                {
                    if (team.Rank == 1)
                        GameVictor = team;

                    continue;
                }

                int earnedScore = CalculateEarnedScore(team);
                team.Score = Math.Min(team.Score + earnedScore, ScoreToWin);

                if (team.Score != previousScore)
                    NotifyScoreChanged(team);
            }

            return GameVictor;
        }

        private int CalculateEarnedScore(PlayerTeam team)
        {
            if (team == null)
                return 0;

            return GetScorePerRank(team.Rank) + CalculateKillBonus(team);
        }

        private int GetScorePerRank(int teamRank)
        {
            if (teamRank <= 0)
                return 0;

            int playerCount = _teams?.Count ?? 0;

            return _data.GetPlacementReward(playerCount, teamRank).Score;
        }

        private int CalculateKillBonus(PlayerTeam team)
        {
            int killBonusScore = 0;

            foreach (PlayerManager member in team.Members)
            {
                var roundKills = member.Metrics.RoundKills;

                if (roundKills == null)
                    continue;

                foreach (E_DeathCause deathCause in roundKills)
                {
                     killBonusScore += _data.GetKillReward(deathCause).Score;
                     Logs.LogWarning(_data.GetKillReward(deathCause).Score.ToString());
                }
                   
            }

            return killBonusScore;
        }

        private void NotifyScoreChanged(PlayerTeam team)
        {
            if (_analyticsSystem == null)
                return;

            if (team.Members == null || team.Members.Count == 0)
                return;

            PlayerManager firstMember = team.Members[0];
            if (firstMember == null || firstMember.Character == null)
                return;

            _analyticsSystem.OnScoreChanged(firstMember.Character, team.Score);
        }

        public List<PlayerTeam> GetOrderWinners(ReadOnlyCollection<PlayerTeam> teams) => teams.OrderByDescending(t => t.Score).ToList();

        public void UpdatePlayerVisualsAfterRound(ReadOnlyCollection<PlayerTeam> teams)
        {
            if (teams == null || teams.Count == 0)
            {
                Logs.LogWarning("No teams detected; cancelling player visuals update");
                return;
            }

            List<PlayerTeam> winnerTeams = GetLeadingTeams(teams);

            if (winnerTeams.Count == 0)
            {
                Logs.LogWarning("No winners detected; cancelling player visuals update");
                return;
            }

            foreach (PlayerTeam team in teams)
            {
                bool isWinnerTeam = winnerTeams.Contains(team);

                foreach (PlayerManager member in team.Members)
                {
                    if (!member || !member.Character || !member.Character.CustomizationVisual)
                        continue;

                    if (isWinnerTeam)
                    {
                        member.Character.CustomizationVisual.UpdateVisualsAfterRound(isWinningGame: true);
                    }
                    else
                    {
                        member.Character.CustomizationVisual.ResetVisualAfterRound();
                    }
                }
            }
        }
        
        private static List<PlayerTeam> GetLeadingTeams(ReadOnlyCollection<PlayerTeam> teams)
        {
            var leadingTeams = new List<PlayerTeam>();

            if (teams == null || teams.Count == 0)
                return leadingTeams;

            int maxScore = teams.Max(team => team.Score);

            foreach (PlayerTeam team in teams)
            {
                if (team.Score == maxScore)
                    leadingTeams.Add(team);
            }

            return leadingTeams;
        }
    }
}