using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Linq;
using MortierFu.Shared;
using System.Collections.Generic;
using PlasticPipe.PlasticProtocol.Messages;

namespace MortierFu.Analytics
{
    public partial class AnalyticsSystem
    {
        private const string GOOGLE_SHEETS_URL = "https://script.google.com/macros/s/AKfycbweVP5xpPXn1yIb4mxnllOAtJM8LTol0cVZU5_Unl4Q--GwPC3WhOXVvPjAMfwlgJSF/exec";

        private async UniTask SendGameOverviewToGoogleSheets()
        {
            if (ShouldSkipAnalyticsInEditor())
            {
                Logs.Log("Analytics send skipped in editor.");
                return;
            }

            try
            {
                WWWForm form = new WWWForm();
                form.AddField("dataType", "game");
                form.AddField("gameId", _gameData.gameId);
                form.AddField("date", _gameData.date);
                form.AddField("gameVersion", _gameData.gameVersion);
                form.AddField("officialGameVersion", _gameData.officialGameVersion);
                form.AddField("durationSeconds", _gameData.durationSeconds.ToString());
                form.AddField("numberOfPlayers", _gameData.numberOfPlayers.ToString());
                form.AddField("roundsPlayed", _gameData.roundsPlayed.ToString());
                form.AddField("scoreToWin", _gameData.scoreToWin.ToString());
                form.AddField("winner", _gameData.winner);

                form.AddField("totalBombshellKills", _gameData.totalBombshellKills.ToString());
                form.AddField("totalSuicides", _gameData.totalSuicides.ToString());
                form.AddField("totalPushKills", _gameData.totalPushKills.ToString());
                form.AddField("totalSelfFalls", _gameData.totalSelfFalls.ToString());

                for (int i = 0; i < 4; i++)
                {
                    string prefix = $"player{i}";

                    if (_gameData.finalPlayerStats != null && i < _gameData.finalPlayerStats.Length)
                    {
                        var stats = _gameData.finalPlayerStats[i];
                        form.AddField($"{prefix}Score", stats.score.ToString());
                        form.AddField($"{prefix}Kills", stats.kills.ToString());
                        form.AddField($"{prefix}Dashes", stats.dashesPerformed.ToString());
                        form.AddField($"{prefix}Bumps", stats.bumpsMade.ToString());
                        form.AddField($"{prefix}StunsPerformed", stats.stunsPerformed.ToString());
                        form.AddField($"{prefix}StunsUnderwented", stats.stunsUnderwented.ToString());
                        form.AddField($"{prefix}ShotsFired", stats.shotsFired.ToString());
                        form.AddField($"{prefix}ShotsHit", stats.shotsHit.ToString());
                        form.AddField($"{prefix}DamageDealt", stats.damageDealt.ToString("F2"));
                        form.AddField($"{prefix}DamageTaken", stats.damageTaken.ToString("F2"));
                    }
                    else
                    {
                        form.AddField($"{prefix}Score", "");
                        form.AddField($"{prefix}Kills", "");
                        form.AddField($"{prefix}Dashes", "");
                        form.AddField($"{prefix}Bumps", "");
                        form.AddField($"{prefix}StunsPerformed", "");
                        form.AddField($"{prefix}StunsUnderwented", "");
                        form.AddField($"{prefix}ShotsFired", "");
                        form.AddField($"{prefix}ShotsHit", "");
                        form.AddField($"{prefix}DamageDealt", "");
                        form.AddField($"{prefix}DamageTaken", "");
                    }
                }

                await AnalyticsNetwork.SendFormWithRedirectHandling(GOOGLE_SHEETS_URL, form, "GameOverview");
            }
            catch (System.Exception ex)
            {
                Logs.LogError($"Exception while sending game summary to Google Sheets: {ex.Message}");
            }
        }

        private async UniTask SendAugmentStatsToGoogleSheets()
        {
            if (ShouldSkipAnalyticsInEditor())
            {
                Logs.Log("Analytics augment send skipped in editor.");
                return;
            }

            if (_augmentStats == null) return;

            try
            {
                WWWForm form = new WWWForm();
                form.AddField("dataType", "augments");
                form.AddField("gameId", _gameData.gameId);
                form.AddField("date", _gameData.date);
                form.AddField("gameVersion", _gameData.gameVersion);
                form.AddField("officialGameVersion", _gameData.officialGameVersion);
                form.AddField("durationSeconds", _gameData.durationSeconds.ToString());
                form.AddField("numberOfPlayers", _gameData.numberOfPlayers.ToString());
                form.AddField("roundsPlayed", _gameData.roundsPlayed.ToString());

                foreach (var entry in _augmentStats.Values.OrderBy(e => e.augmentId))
                {
                    form.AddField($"augment{entry.augmentId}_Shown", entry.timesShown.ToString());
                    form.AddField($"augment{entry.augmentId}_Picked", entry.timesPicked.ToString());
                    form.AddField($"augment{entry.augmentId}_PickedByWinner", entry.timesPickedByWinner.ToString());
                    form.AddField($"augment{entry.augmentId}_WinnerHadIt", entry.winnerHadIt.ToString().ToUpper());
                }

                await AnalyticsNetwork.SendFormWithRedirectHandling(GOOGLE_SHEETS_URL, form, "augment-stats");
            }
            catch (System.Exception ex)
            {
                Logs.LogError($"Exception while sending augment stats: {ex.Message}");
            }
        }
        
        private async UniTask SendAllRoundsOverviewToGoogleSheets()
        {
            if (ShouldSkipAnalyticsInEditor())
            {
                Logs.Log("Analytics rounds overview send skipper in editor.");
                return;
            }

            for (int i = 0; i < _gameData.roundsPlayed; i++)
            {
                var round = _gameData.rounds[i];
                if (round?.players == null) continue;

                await SendSingleRoundOverview(round);
            }
        }

        private async UniTask SendSingleRoundOverview(AnalyticsRoundData round)
        {
            try
                {
                    WWWForm form = new WWWForm();
                    form.AddField("dataType", "roundOverview");
                    form.AddField("gameId", _gameData.gameId);
                    form.AddField("date", _gameData.date);
                    form.AddField("devVersion", _gameData.gameVersion);
                    form.AddField("gameVersion", _gameData.officialGameVersion);

                    form.AddField("nbrPlayer", _gameData.numberOfPlayers.ToString());
                    form.AddField("roundNumber", round.roundNumber.ToString());
                    form.AddField("roundDuration", round.roundDurationSeconds.ToString());
                    form.AddField("roundWinner", round.roundWinner);

                    var sortedPlayers = round.players.OrderBy(p => p.playerId).Take(4).ToList();

                    for (int p = 0; p < 4; p++)
                    {
                        string prefix = $"player{p}";

                        if (p < sortedPlayers.Count)
                        {
                            var player = sortedPlayers[p];

                            form.AddField($"{prefix}ScoreAtEnd", player.score.ToString());
                            form.AddField($"{prefix}LastAugmentPicked", player.selectedAugment != null ? player.selectedAugment.Name : "-");
                            form.AddField($"{prefix}Kills", player.kills.ToString());
                            form.AddField($"{prefix}Dashes", player.dashesPerformed.ToString());
                            form.AddField($"{prefix}Bumped", player.bumpsMade.ToString());
                            form.AddField($"{prefix}Stun", player.stunsPerformed.ToString());
                            form.AddField($"{prefix}Stunned", player.stunsUnderwented.ToString());
                            form.AddField($"{prefix}ShotFired", player.shotsFired.ToString());
                            form.AddField($"{prefix}ShotHit", player.shotsHit.ToString());
                            form.AddField($"{prefix}DamageDealt", player.damageDealt.ToString("F2"));
                            form.AddField($"{prefix}Taken", player.damageTaken.ToString("F2"));
                            form.AddField($"{prefix}DeathCause", player.playerId == round.roundWinner ? "Win" : ShortenDeathCause(player.deathCause));
                        }
                        else
                        {
                            form.AddField($"{prefix}ScoreAtEnd", "");
                            form.AddField($"{prefix}LastAugmentPicked", "");
                            form.AddField($"{prefix}Kills", "");
                            form.AddField($"{prefix}Dashes", "");
                            form.AddField($"{prefix}Bumped", "");
                            form.AddField($"{prefix}Stun", "");
                            form.AddField($"{prefix}Stunned", "");
                            form.AddField($"{prefix}ShotFired", "");
                            form.AddField($"{prefix}ShotHit", "");
                            form.AddField($"{prefix}DamageDealt", "");
                            form.AddField($"{prefix}Taken", "");
                            form.AddField($"{prefix}DeathCause", "");
                        }
                    }

                    await AnalyticsNetwork.SendFormWithRedirectHandling(GOOGLE_SHEETS_URL, form,
                        $"round-{round.roundNumber}");
                }
                catch (System.Exception ex)
                {
                    Logs.LogError($"Exception while sending round overview ; {ex.Message}");
                }
        }

        private async UniTask SendPlayerStatsToGoogleSheets()
        {
            if (ShouldSkipAnalyticsInEditor())
            {
                Logs.Log("Analytics player stats send skipped in editr");
                return;
            }

            try
            {
                WWWForm form = new WWWForm();
                form.AddField("dataType", "playerStats");
                form.AddField("gameId", _gameData.gameId);
                form.AddField("date", _gameData.date);
                form.AddField("devVersion", _gameData.gameVersion);
                form.AddField("officialGameVersion", _gameData.officialGameVersion);
                form.AddField("durationSeconds", _gameData.durationSeconds.ToString());
                form.AddField("numberOfPlayers", _gameData.numberOfPlayers.ToString());
                form.AddField("roundsPlayed", _gameData.roundsPlayed.ToString());
                form.AddField("winner", _gameData.winner);

                for (int i = 0; i < 4; i++)
                {
                    string prefix = $"player{i}";

                    if (_gameData.finalPlayerStats != null && i < _gameData.finalPlayerStats.Length &&
                        _gameData.finalPlayerStats[i].playerStats != null)
                    {
                        var player = _gameData.finalPlayerStats[i].playerStats;
                        form.AddField($"{prefix}MaxHealth", player.maxHealth.ToString("F2"));
                        form.AddField($"{prefix}MoveSpeed", player.moveSpeed.ToString("F2"));
                        form.AddField($"{prefix}BombshellDamage", player.bombshellDamage.ToString("F2"));
                        form.AddField($"{prefix}ImpactRadius", player.bombshellImpactRadius.ToString("F2"));
                        form.AddField($"{prefix}BulletSpeed", player.bombshellSpeed.ToString("F2"));
                        form.AddField($"{prefix}FireRate", player.fireRate.ToString("F2"));
                        form.AddField($"{prefix}ShotRange", player.shotRange.ToString("F2"));
                        form.AddField($"{prefix}DashCharges", player.dashCharges.ToString());
                        form.AddField($"{prefix}DashCooldown", player.dashCooldown.ToString("F2"));
                        form.AddField($"{prefix}DashDistance", player.dashDistance.ToString());
                        form.AddField($"{prefix}StrikePushForce", player.strikePushForce.ToString());
                        form.AddField($"{prefix}StunDuration", player.strikeStunDuration.ToString());
                    }
                    else
                    {
                        form.AddField($"{prefix}MaxHealth", "");
                        form.AddField($"{prefix}MoveSpeed", "");
                        form.AddField($"{prefix}BombshellDamage", "");
                        form.AddField($"{prefix}ImpactRadius", "");
                        form.AddField($"{prefix}BulletSpeed", "");
                        form.AddField($"{prefix}FireRate", "");
                        form.AddField($"{prefix}ShotRange", "");
                        form.AddField($"{prefix}DashCharges", "");
                        form.AddField($"{prefix}DashCooldown", "");
                        form.AddField($"{prefix}DashDistance", "");
                        form.AddField($"{prefix}StrikePushForce", "");
                        form.AddField($"{prefix}StunDuration", "");
                    }
                }
                await AnalyticsNetwork.SendFormWithRedirectHandling(GOOGLE_SHEETS_URL, form, "playerStats");
            }
            catch (System.Exception ex)
            {
                Logs.LogError($"Exception while sending player stats ; {ex.Message}");
            }
        }
    }
}