using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Linq;
using MortierFu.Shared;
using UnityEditor;
using UnityEngine.UIElements;

namespace MortierFu.Analytics
{
    public class AnalyticsSystem : IGameSystem
    {
        private const string GOOGLE_SHEETS_URL = "https://script.google.com/macros/s/AKfycbweVP5xpPXn1yIb4mxnllOAtJM8LTol0cVZU5_Unl4Q--GwPC3WhOXVvPjAMfwlgJSF/exec";
        
        private EventBinding<TriggerHit> _triggerHitBinding;
        private EventBinding<TriggerShootBombshell> _triggerShootBombshellBinding;
        private EventBinding<TriggerHealthChanged> _triggerHealthChangedBinding;
        private EventBinding<TriggerDash> _triggerDashBinding;
        private EventBinding<TriggerStrike> _triggerStrikeBinding;
        private EventBinding<EventPlayerDeath> _triggerDeathBinding;
        private EventBinding<TriggerEndRound> _triggerEndRoundBinding;
        private EventBinding<TriggerSuccessfulPush> _triggerSuccessfulPushBinding;
        
        private AnalyticsData _gameData;
        private int _currentRoundIndex = 0;
        private GameState _currentGameState;
        private Dictionary<string, AnalyticsPlayerData> _currentRoundPlayers;
        
        public UniTask OnInitialize()
        {
            RegisterEvents();
            CreateNewGameData();
            IsInitialized = true;
            return UniTask.CompletedTask;
        }

        private void RegisterEvents()
        {
            _triggerShootBombshellBinding = new EventBinding<TriggerShootBombshell>(OnTriggerShootBombshell);
            EventBus<TriggerShootBombshell>.Register(_triggerShootBombshellBinding);
            
            _triggerHitBinding = new EventBinding<TriggerHit>(OnTriggerHit);
            EventBus<TriggerHit>.Register(_triggerHitBinding);
            
            _triggerHealthChangedBinding = new EventBinding<TriggerHealthChanged>(OnTriggerHealthChanged);
            EventBus<TriggerHealthChanged>.Register(_triggerHealthChangedBinding);
            
            _triggerDashBinding = new EventBinding<TriggerDash>(OnTriggerDash);
            EventBus<TriggerDash>.Register(_triggerDashBinding);
            
            _triggerStrikeBinding = new EventBinding<TriggerStrike>(OnTriggerStrike);
            EventBus<TriggerStrike>.Register(_triggerStrikeBinding);
            
            _triggerDeathBinding = new EventBinding<EventPlayerDeath>(OnTriggerDeath);
            EventBus<EventPlayerDeath>.Register(_triggerDeathBinding);
            
            _triggerEndRoundBinding = new EventBinding<TriggerEndRound>(OnTriggerEndRound);
            EventBus<TriggerEndRound>.Register(_triggerEndRoundBinding);

            _triggerSuccessfulPushBinding = new EventBinding<TriggerSuccessfulPush>(OnTriggerSuccessfulPush);
            EventBus<TriggerSuccessfulPush>.Register(_triggerSuccessfulPushBinding);

            if (GameService.CurrentGameMode != null)
            {
                GameService.CurrentGameMode.OnGameEnded += OnGameEndedHandler;
                GameService.CurrentGameMode.OnGameStateChanged += OnGameStateChangedHandler;
                _currentGameState = GameService.CurrentGameMode.CurrentGameState;
            }
            else
                Logs.LogWarning("[AnalyticsSystem] CurrentGameMode est null à l'initialisation, donc pas reçu");
            
        }

        private void OnGameStateChangedHandler(GameState newState)
        {
            _currentGameState = newState;
            Logs.Log($"[DEBUG] GameState changé vers : {newState}");
           
        }

        private bool IsInCombatPhase()
        {
            return _currentGameState != GameState.AugmentIntro && _currentGameState != GameState.AugmentRace && _currentGameState != GameState.EndAugmentRace;
        }
        private System.DateTime _gameStartTime;
        
        private void CreateNewGameData()
        {
            _gameStartTime = System.DateTime.UtcNow;
            _gameData = new AnalyticsData()
            {
                gameId = System.Guid.NewGuid().ToString(),
                date = System.DateTime.UtcNow.ToString("yyyy-MM-ddT HH:mm:ss"),
                numberOfPlayers = ServiceManager.Instance.Get<LobbyService>().CurrentPlayerCount,
                gameVersion = Application.version,
                scoreToWin = (GameService.CurrentGameMode as GameModeBase)?.ScoreToWin ?? 0,
                officialGameVersion = "b.1.1",
                rounds = new AnalyticsRoundData[1000], // Taille max de rounds
                winner = "",
                roundsPlayed = 0,
            };
            
            StartNewRound();
        }

        private void StartNewRound()
        {
            _currentRoundPlayers = new Dictionary<string, AnalyticsPlayerData>();
            
            var currentRound = new AnalyticsRoundData()
            {
                roundNumber = _currentRoundIndex + 1,
                roundWinner = "",
                players = new List<AnalyticsPlayerData>()
            };
            
            _gameData.rounds[_currentRoundIndex] = currentRound;
            
            // Initialiser les données des joueurs pour ce round
            InitializePlayersForRound();
        }

        private void InitializePlayersForRound()
        {
            // Récupérer tous les joueurs actifs
            var lobbyService = ServiceManager.Instance.Get<LobbyService>();
            var players = lobbyService.GetPlayers(); 
            
            foreach (var player in players)
            {
                string playerId = GetPlayerIdFromCharacter(player);
                var playerData = new AnalyticsPlayerData()
                {
                    playerId = playerId,
                    rank = 0,
                    score = 0,
                    kills = 0,
                    selectedAugment = null, // À récupérer depuis le joueur
                    damageDealt = 0f,
                    damageTaken = 0f,
                    shotsFired = 0,
                    shotsHit = 0,
                    dashesPerformed = 0,
                    bumpsMade = 0,
                    killerId = -1,
                    deathCause = E_DeathCause.Unknown
                };
                
                _currentRoundPlayers[playerId] = playerData;
            }
        }

        private string GetPlayerIdFromCharacter(PlayerManager character)
        {
            return character.PlayerIndex.ToString();
        }

        private AnalyticsPlayerData GetOrCreatePlayerData(PlayerCharacter character)
        {
            string playerId = GetPlayerIdFromCharacter(character.Owner);
            
            if (!_currentRoundPlayers.ContainsKey(playerId))
            {
                var newPlayerData = new AnalyticsPlayerData()
                {
                    playerId = playerId,
                    rank = 0,
                    score = 0,
                    kills = 0,
                    selectedAugment = null,
                    damageDealt = 0f,
                    damageTaken = 0f,
                    shotsFired = 0,
                    shotsHit = 0,
                    dashesPerformed = 0,
                    bumpsMade = 0,
                    killerId = -1,
                    deathCause = E_DeathCause.Unknown
                };
                _currentRoundPlayers[playerId] = newPlayerData;
            }
            
            return _currentRoundPlayers[playerId];
        }

        private void OnTriggerShootBombshell(TriggerShootBombshell shootBombshell)
        {
            if (!IsInCombatPhase()) return;
            if (shootBombshell.Character == null) return;
            
            var playerData = GetOrCreatePlayerData(shootBombshell.Character);
            playerData.shotsFired++;
        }

        private void OnTriggerHit(TriggerHit hit)
        {
            if (!IsInCombatPhase()) return;
            if (hit.ShooterId == null) return;
            
            var shooterData = GetOrCreatePlayerData(hit.ShooterId);
            
            // Incrémenter shots hit si au moins un personnage a été touché
            if (hit.HitCharacters != null && hit.HitCharacters.Length > 0)
            {
                shooterData.shotsHit++;
            }
        }
        
        private void OnTriggerHealthChanged(TriggerHealthChanged healthChanged)
        {
            // Gérer les dégâts infligés
            if (healthChanged.Instigator != null && healthChanged.Delta < 0)
            {
                var instigatorData = GetOrCreatePlayerData(healthChanged.Instigator);
                instigatorData.damageDealt += Mathf.Abs(healthChanged.Delta);
            }
            
            // Gérer les dégâts reçus
            if (healthChanged.Character != null && healthChanged.Delta < 0)
            {
                var victimData = GetOrCreatePlayerData(healthChanged.Character);
                victimData.damageTaken += Mathf.Abs(healthChanged.Delta);
            }
        }
        
        private void OnTriggerDash(TriggerDash dash)
        {
            if (!IsInCombatPhase()) return;
            if (dash.Character == null) return;
            
            var playerData = GetOrCreatePlayerData(dash.Character);
            playerData.dashesPerformed++;
        }
        
        private void OnTriggerStrike(TriggerStrike strike)
        {
            if (!IsInCombatPhase()) return;
            if (strike.Character == null) return;
            
            var playerData = GetOrCreatePlayerData(strike.Character);
            
            // Compter le nombre de bumps (coups réussis)
            if (strike.HitCharacters != null)
            {
                playerData.bumpsMade += strike.HitCharacters.Length;
            }
        }
        
        private void OnTriggerDeath(EventPlayerDeath death)
        {
            if (death.Character == null) return;
            
            var victimData = GetOrCreatePlayerData(death.Character);

            if (death.Context.Killer) {
                var killerData = GetOrCreatePlayerData(death.Context.Killer);
                killerData.kills++;
            }

            if (death.Context.Killer)
                victimData.killerId = death.Context.Killer.Owner.PlayerIndex;
            victimData.deathCause = death.Context.DeathCause;
        }
        
        private void OnTriggerEndRound(TriggerEndRound endRound)
        {
            FinalizeCurrentRound();
            
            _currentRoundIndex++;
            _gameData.roundsPlayed++;
            
            StartNewRound();
        }

        private void OnTriggerSuccessfulPush(TriggerSuccessfulPush push)
        {
            if (!IsInCombatPhase()) return;
            if (push.Character == null) return;
            
            var victimData = GetOrCreatePlayerData(push.Character);
            victimData.stunsUnderwented++;

            if (push.Source is PlayerCharacter instigator && instigator != push.Character)
            {
                var instigatorData = GetOrCreatePlayerData(instigator);
                instigatorData.stunsPerformed++;
            }
        }

        private void FinalizeCurrentRound()
        {
            var currentRound = _gameData.rounds[_currentRoundIndex];
            
            currentRound.players = _currentRoundPlayers.Values.ToList();
            
            var winner = currentRound.players.OrderByDescending(p => p.kills)
                                            .ThenByDescending(p => p.score)
                                            .FirstOrDefault();
            
            if (winner != null)
            {
                currentRound.roundWinner = winner.playerId;
            }
            
            // Assigner les rangs
            AssignRanks(currentRound.players);
        }
        
        private void OnGameEndedHandler(int winnerPlayerIndex)
        {
            var duration = System.DateTime.UtcNow - _gameStartTime;
            _gameData.durationSeconds = (int)duration.TotalSeconds;
    
            FinalizeGame();

            SendGameOverviewToGoogleSheets().Forget();
            //SendAllRoundsToGoogleSheets().Forget();
        }

        private async UniTask SendAllRoundsToGoogleSheets()
        {
            for (int i = 0; i < _gameData.roundsPlayed; i++)
            {
                var round = _gameData.rounds[i];
                if (round == null) continue;
        
                await SendRoundDataToGoogleSheets(round);
            }
        }
        
        private void AssignRanks(List<AnalyticsPlayerData> players)
        {
            var sortedPlayers = players.OrderByDescending(p => p.score)
                                      .ThenByDescending(p => p.kills)
                                      .ToList();
            
            for (int i = 0; i < sortedPlayers.Count; i++)
            {
                sortedPlayers[i].rank = i + 1;
            }
        }

        public void OnAugmentSelected(PlayerCharacter character, SO_Augment augment = null)
        {
            var playerData = GetOrCreatePlayerData(character);
            playerData.selectedAugment = augment;
        }
        
        public void OnScoreChanged(PlayerCharacter character, int newScore)
        {
            var playerData = GetOrCreatePlayerData(character);
            playerData.score = newScore;
            Logs.Log($"[DEBUG] OnScoreChanged appelé pour {playerData.playerId}, nouveau score = {newScore}, round actuel = {_currentRoundIndex + 1}");
        }
        
        private void AggregateFinalStats()
        {
        var statsByPlayer = new Dictionary<string, AnalyticsFinalPlayerStats>();
        
        _gameData.totalBombshellKills = 0;
        _gameData.totalSuicides = 0;
        _gameData.totalPushKills = 0;
        _gameData.totalSelfFalls = 0;
        
        // Étape 1 : agréger kills/dashes/etc. depuis les rounds (comme avant)
        for (int i = 0; i < _gameData.roundsPlayed; i++)
        {
            var round = _gameData.rounds[i];
            if (round?.players == null) continue;
        
            foreach (var player in round.players)
            {
                if (!statsByPlayer.TryGetValue(player.playerId, out var stats))
                {
                    stats = new AnalyticsFinalPlayerStats { playerId = player.playerId };
                    statsByPlayer[player.playerId] = stats;
                }
            
                stats.kills += player.kills;
                stats.dashesPerformed += player.dashesPerformed;
                stats.bumpsMade += player.bumpsMade;
                stats.shotsFired += player.shotsFired;
                stats.shotsHit += player.shotsHit;
                stats.damageDealt += player.damageDealt;
                stats.damageTaken += player.damageTaken;
                stats.stunsPerformed += player.stunsPerformed;
                stats.stunsUnderwented += player.stunsUnderwented;
            
                bool killedBySomeoneElse = player.killerId != -1 
                    && player.killerId.ToString() != player.playerId;
            
                if (player.deathCause == E_DeathCause.BombshellExplosion)
                {
                    if (killedBySomeoneElse) _gameData.totalBombshellKills++;
                    else _gameData.totalSuicides++;
                }
                else if (player.deathCause == E_DeathCause.Fall)
                {
                    if (killedBySomeoneElse) _gameData.totalPushKills++;
                    else _gameData.totalSelfFalls++;
                }
            }
        }
        
        var gameMode = GameService.CurrentGameMode as GameModeBase;
        if (gameMode != null)
            { 
                 foreach (var team in gameMode.Teams) 
                {
                     int teamScore = team.Score;
            
                    foreach (var member in team.Members)
                         { 
                             if (member?.Character == null) continue;
                
                             string playerId = GetPlayerIdFromCharacter(member);
                
                             if (!statsByPlayer.TryGetValue(playerId, out var stats))
                                {
                                    stats = new AnalyticsFinalPlayerStats { playerId = playerId };
                                    statsByPlayer[playerId] = stats;
                                }
                
                            stats.score = teamScore;
                         }
                 }
            }
        else
            {
                 Logs.LogWarning("[AnalyticsSystem] Impossible de caster CurrentGameMode en GameModeBase, scores finaux non lus depuis Teams."); 
            }
    
        _gameData.finalPlayerStats = statsByPlayer.Values
        .OrderBy(s => s.playerId)
        .Take(4)
        .ToArray();
        }    
        private void FinalizeGame()
        {
            // Déterminer le gagnant global
            GameService.CurrentGameMode.IsGameOver(out var playerWins);
            
            if (playerWins != null)
            {
                string winnerId = GetPlayerIdFromCharacter(playerWins.Members[0]);
                _gameData.winner = winnerId;
            }
            
            AggregateFinalStats();
            
            // Exporter vers Excel (backup local)
            ExportToExcel();
        }
        private async UniTask<bool> SendFormWithRedirectHandling(string url, WWWForm form, string playerId)
        {
            using (UnityWebRequest www = UnityWebRequest.Post(url, form))
            {
                www.redirectLimit = 0;

                try
                {
                    await www.SendWebRequest();

                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        Logs.Log($"Successfully sent data for player {playerId} to Google Sheets");
                        return true;
                    }

                    Logs.LogError($"Error sending data to Google Sheets: {www.error}");
                    return false;
                }
                catch (System.Exception)
                {
                    string redirectUrl = www.GetResponseHeader("Location");
                    if (string.IsNullOrEmpty(redirectUrl))
                    {
                        Logs.LogError("Redirection détectée mais impossible de récupérer l'URL de destination.");
                        return false;
                    }

                    using (UnityWebRequest redirected = UnityWebRequest.Get(redirectUrl))
                    {
                        await redirected.SendWebRequest();

                        if (redirected.result != UnityWebRequest.Result.Success)
                        {
                            Logs.LogError($"Error (redirected): {redirected.error}");
                            return false;
                        }

                        Logs.Log($"Successfully sent data for player {playerId} to Google Sheets (redirected). Response: {redirected.downloadHandler.text}");
                        return true;
                    }
                }
            }
        }

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
                // Global Game Innfo
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
                
                // Total Kills Actions
                form.AddField("totalBombshellKills", _gameData.totalBombshellKills.ToString());
                form.AddField("totalSuicides", _gameData.totalSuicides.ToString());
                form.AddField("totalPushKills", _gameData.totalPushKills.ToString());
                form.AddField("totalSelfFalls", _gameData.totalSelfFalls.ToString());

                // Players Summary
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

                    await SendFormWithRedirectHandling(GOOGLE_SHEETS_URL, form, "GameOverview");
             }
            catch (System.Exception ex)
            {
                 Logs.LogError($"Exception while sending game summary to Google Sheets: {ex.Message}");
            }
        }
        private async UniTask SendRoundDataToGoogleSheets(AnalyticsRoundData roundData)
        {
            if (roundData == null || roundData.players == null) return;
            
            if (ShouldSkipAnalyticsInEditor())
            {
                Logs.Log("Analytics send skipped in editor.");
                return;
            }
            
            foreach (var player in roundData.players)
            {
                try
                {
                    WWWForm form = new WWWForm();
                    
                    // Ajouter toutes les données selon le format attendu par Google Sheets
                    form.AddField("gameId", _gameData.gameId);
                    form.AddField("date", _gameData.date);
                    form.AddField("gameVersion", _gameData.gameVersion);
                    form.AddField("officialGameVersion", _gameData.officialGameVersion.ToString());
                    form.AddField("durationSeconds", _gameData.durationSeconds.ToString());
                    form.AddField("numberOfPlayers", _gameData.numberOfPlayers.ToString());
                    form.AddField("totalRounds", _gameData.roundsPlayed.ToString());
                    form.AddField("scoreToWin", _gameData.scoreToWin);
                    form.AddField("winner", _gameData.winner);
                    form.AddField("roundNumber", roundData.roundNumber.ToString());
                    form.AddField("roundWinner", roundData.roundWinner);
                    form.AddField("playerId", player.playerId);
                    form.AddField("rank", player.rank.ToString());
                    form.AddField("score", player.score.ToString());
                    form.AddField("kills", player.kills.ToString());
                    form.AddField("augment", player.selectedAugment != null ? player.selectedAugment.name : "None");
                    form.AddField("damageDealt", player.damageDealt.ToString("F2"));
                    form.AddField("damageTaken", player.damageTaken.ToString("F2"));
                    form.AddField("shotsFired", player.shotsFired.ToString());
                    form.AddField("shotsHit", player.shotsHit.ToString());
                    
                    // Calculer l'accuracy
                    float accuracy = player.shotsFired > 0 ? (float)player.shotsHit / player.shotsFired * 100f : 0f;
                    form.AddField("accuracy", accuracy.ToString("F2"));
                    
                    form.AddField("dashesPerformed", player.dashesPerformed.ToString());
                    form.AddField("bumpsMade", player.bumpsMade.ToString());
                    form.AddField("deathCause", player.deathCause.ToString());

                   await SendFormWithRedirectHandling(GOOGLE_SHEETS_URL, form, player.playerId);
                }
                catch (System.Exception ex)
                {
                    Logs.LogError($"Exception while sending data to Google Sheets: {ex.Message}");
                }
                
                // Petit délai entre chaque requête pour éviter de surcharger l'API
                await UniTask.Delay(100);
            }
        }

        private bool ShouldSkipAnalyticsInEditor()
        {
            #if UNITY_EDITOR
                return Application.isEditor && !UnityEditor.EditorPrefs.GetBool("AnalyticsInEditor", false);
            #else
                return false;
            #endif
        }

        private void ExportToExcel()
        {
            // if in editor return
            if (ShouldSkipAnalyticsInEditor())
            {
                Logs.Log("Analytics export skipped in editor.");
                return;
            }
            
            // Backup local en JSON
            Logs.Log($"Exporting game data: {_gameData.gameId}");
            
            string json = JsonUtility.ToJson(_gameData, true);
            Logs.Log(json);
            
            // Sauvegarder temporairement en JSON
            string path = System.IO.Path.Combine(Application.persistentDataPath, 
                $"GameData_{_gameData.gameId}.json");
            System.IO.File.WriteAllText(path, json);
            Logs.Log($"Data saved to: {path}");
        }

        public bool IsInitialized { get; set; }
        
        public void Dispose()
        {
            // Unregister tous les events
            EventBus<TriggerShootBombshell>.Deregister(_triggerShootBombshellBinding);
            EventBus<TriggerHit>.Deregister(_triggerHitBinding);
            EventBus<TriggerHealthChanged>.Deregister(_triggerHealthChangedBinding);
            EventBus<TriggerDash>.Deregister(_triggerDashBinding);
            EventBus<TriggerStrike>.Deregister(_triggerStrikeBinding);
            EventBus<EventPlayerDeath>.Deregister(_triggerDeathBinding);
            EventBus<TriggerEndRound>.Deregister(_triggerEndRoundBinding);
            EventBus<TriggerSuccessfulPush>.Deregister(_triggerSuccessfulPushBinding);

            if (GameService.CurrentGameMode != null)
            {
                GameService.CurrentGameMode.OnGameEnded -= OnGameEndedHandler;
                GameService.CurrentGameMode.OnGameStateChanged -= OnGameStateChangedHandler;
            }

            
            // Sauvegarder les données avant de disposer
            if (_gameData != null && _gameData.roundsPlayed > 0)
            {
                ExportToExcel();
            }
        }
    }
}