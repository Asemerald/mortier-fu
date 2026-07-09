using System;
using System.Collections.Generic;
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

        public ScoreController(
            SO_GameModeData data,
            IReadOnlyList<PlayerTeam> teams,
            int scoreToWin,
            AnalyticsSystem analyticsSystem = null)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _teams = teams ?? throw new ArgumentNullException(nameof(teams));
            _analyticsSystem = analyticsSystem;

            ScoreToWin = scoreToWin;
            GameVictor = null;
        }

        public void SetScoreToWin(int scoreToWin)
        {
            ScoreToWin = scoreToWin;
        }

        public void Reset()
        {
            GameVictor = null;
        }

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
                    {
                        GameVictor = team;
                    }

                    continue;
                }

                int earnedScore = CalculateEarnedScore(team);
                team.Score = Math.Min(team.Score + earnedScore, ScoreToWin);

                if (team.Score != previousScore)
                {
                    NotifyScoreChanged(team);
                }
            }

            return GameVictor;
        }

        public int CalculateEarnedScore(PlayerTeam team)
        {
            if (team == null)
                return 0;

            return GetScorePerRank(team.Rank) + CalculateKillBonus(team);
        }

        public int GetScorePerRank(int teamRank)
        {
            if (teamRank <= 0)
                return 0;

            int playerCount = _teams?.Count ?? 0;

            if (playerCount > 0)
            {
                if (teamRank == 1 && _data.FirstRankBonusByPlayerCount != null && playerCount < _data.FirstRankBonusByPlayerCount.Length)
                {
                    return _data.FirstRankBonusByPlayerCount[playerCount-1];
                }

                if (teamRank == 2 && _data.SecondRankBonusByPlayerCount != null && playerCount < _data.SecondRankBonusByPlayerCount.Length)
                {
                    return _data.SecondRankBonusByPlayerCount[playerCount-1];
                }

                if (teamRank == 3 && _data.ThirdRankBonusByPlayerCount != null && playerCount < _data.ThirdRankBonusByPlayerCount.Length)
                {
                    return _data.ThirdRankBonusByPlayerCount[playerCount-1];
                }
            }

            return 0;
        }

        private int CalculateKillBonus(PlayerTeam team)
        {
            int killBonusScore = 0;

            foreach (var member in team.Members)
            {
                var roundKills = member.Metrics.RoundKills;
                if (roundKills == null)
                    continue;

                foreach (var deathCause in roundKills)
                {
                    killBonusScore += _data.KillBonusScore;

                    switch (deathCause)
                    {
                        case E_DeathCause.Fall:
                            killBonusScore += _data.KillPushBonusScore;
                            break;

                        case E_DeathCause.VehicleCrash:
                            killBonusScore += _data.KillCarCrashBonusScore;
                            break;
                    }
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

            var firstMember = team.Members[0];
            if (firstMember == null || firstMember.Character == null)
                return;

            _analyticsSystem.OnScoreChanged(firstMember.Character, team.Score);
        }

        public List<PlayerTeam> GetOrderWinners(List<PlayerTeam> teams)
        {
            return teams.OrderByDescending(t => t.Score).ToList();
        }

        public void UpdatePlayerVisualsAfterRound(List<PlayerTeam> teams)
        {
            if (teams == null || teams.Count == 0)
            {
                Logs.LogWarning("No teams detected; cancelling player visuals update");
                return;
            }
            
            int count = teams.Count;
            
            //reset visuals
            for (int i = 0; i < count; i++)
            {
                foreach (var member in teams[i].Members)
                {
                    if (!member || !member.Character || !member.Character.CustomizationVisual)
                        return;
                    
                    member.Character.CustomizationVisual.ResetVisualAfterRound();
                }
            }

            List<PlayerTeam> winners = GetOrderWinners(teams);

            if (winners.Count == 0 || winners[0] == null)
            {
                Logs.LogWarning("No winners detected; cancelling player visuals update");
                return;
            }
            
            PlayerTeam winner = winners[0];

            //apply visuals to winner team
            foreach (var playerWin in winner.Members)
            {
                if (!playerWin || !playerWin.Character || !playerWin.Character.CustomizationVisual)
                    return;
                
                playerWin.Character.CustomizationVisual.UpdateVisualsAfterRound(isWinningGame: true);
            }
        }
    }
}