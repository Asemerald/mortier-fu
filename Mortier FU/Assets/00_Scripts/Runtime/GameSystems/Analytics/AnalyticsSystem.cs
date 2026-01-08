using Codice.Client.BaseCommands;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace MortierFu.Analytics
{
    public class AnalyticsSystem : IGameSystem
    {
        private EventBinding<TriggerHit> _triggerHitBinding;
        private EventBinding<TriggerShootBombshell> _triggerShootBombshellBinding;
        private EventBinding<TriggerHealthChanged> _triggerHealthChangedBinding;
        private EventBinding<TriggerStrike> _triggerStrikeBinding;
        private EventBinding<TriggerStrikeHit> _triggerStrikeHitBinding;
        private EventBinding<EventPlayerDeath> _triggerDeathBinding;
        private EventBinding<TriggerEndRound> _triggerEndRoundBinding;
        
        private AnalyticsData _gameData;
        private int _currentRoundIndex = 0;
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
            
            _triggerStrikeBinding = new EventBinding<TriggerStrike>(OnTriggerStrike);
            EventBus<TriggerStrike>.Register(_triggerStrikeBinding);
            
            _triggerStrikeHitBinding = new EventBinding<TriggerStrikeHit>(OnTriggerStrikeHit);
            EventBus<TriggerStrikeHit>.Register(_triggerStrikeHitBinding);
            
            _triggerDeathBinding = new EventBinding<EventPlayerDeath>(OnTriggerDeath);
            EventBus<EventPlayerDeath>.Register(_triggerDeathBinding);
            
            _triggerEndRoundBinding = new EventBinding<TriggerEndRound>(OnTriggerEndRound);
            EventBus<TriggerEndRound>.Register(_triggerEndRoundBinding);
        }
        
        private void CreateNewGameData()
        {
            _gameData = new AnalyticsData()
            {
                gameId = System.Guid.NewGuid().ToString(),
                date = System.DateTime.UtcNow.ToString("o"),
                numberOfPlayers = ServiceManager.Instance.Get<LobbyService>().CurrentPlayerCount,
                gameVersion = Application.version,
                rounds = new AnalyticsRoundData[15], // Taille max de rounds
                winner = "",
                roundsPlayed = 0
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
                    deathCause = DeathCause.None
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
                    deathCause = DeathCause.None
                };
                _currentRoundPlayers[playerId] = newPlayerData;
            }
            
            return _currentRoundPlayers[playerId];
        }

        private void OnTriggerShootBombshell(TriggerShootBombshell shootBombshell)
        {
            if (shootBombshell.Character == null) return;
            
            var playerData = GetOrCreatePlayerData(shootBombshell.Character);
            playerData.shotsFired++;
        }

        private void OnTriggerHit(TriggerHit hit)
        {
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
        
        private void OnTriggerStrike(TriggerStrike strike)
        {
            if (strike.Character == null) return;
            
            var playerData = GetOrCreatePlayerData(strike.Character);
            playerData.dashesPerformed++;
        }
        
        private void OnTriggerStrikeHit(TriggerStrikeHit strikeHit)
        {
            if (strikeHit.Character == null) return;
            
            var playerData = GetOrCreatePlayerData(strikeHit.Character);
            
            // Compter le nombre de bumps (coups réussis)
            if (strikeHit.HitCharacters != null)
            {
                playerData.bumpsMade += strikeHit.HitCharacters.Length;
            }
        }
        
        private void OnTriggerDeath(EventPlayerDeath death)
        {
            if (death.Character == null) return;
            
            var victimData = GetOrCreatePlayerData(death.Character);
            
            // Déterminer la cause de mort et incrémenter les kills si nécessaire
            if (death.Source is PlayerCharacter killer)
            {
                // Mort causée par un autre joueur
                var killerData = GetOrCreatePlayerData(killer);
                killerData.kills++;
                
                victimData.deathCause = GetDeathCauseFromPlayer(killer);
            }
            else if (death.Source is Bombshell bombshell)
            {
                // Mort causée par un projectile
                if (bombshell.Owner != null)
                {
                    var killerData = GetOrCreatePlayerData(bombshell.Owner);
                    killerData.kills++;
                    victimData.deathCause = GetDeathCauseFromPlayer(bombshell.Owner);
                }
                else
                {
                    victimData.deathCause = DeathCause.Fall;
                }
            }
            else
            {
                // Mort par l'environnement (murs, obstacles, etc.)
                victimData.deathCause = DeathCause.Fall;
            }
        }

        private DeathCause GetDeathCauseFromPlayer(PlayerCharacter player)
        {
             return (DeathCause)(player.Owner.PlayerIndex + 1);
        }
        
        private void OnTriggerEndRound(TriggerEndRound endRound)
        {
            FinalizeCurrentRound();
            
            _currentRoundIndex++;
            _gameData.roundsPlayed++;
            
            // Si le jeu continue, démarrer un nouveau round
            if (!IsGameOver())
            {
                StartNewRound();
            }
            else
            {
                FinalizeGame();
            }
        }

        private void FinalizeCurrentRound()
        {
            var currentRound = _gameData.rounds[_currentRoundIndex];
            
            // Ajouter tous les joueurs du round
            currentRound.players = _currentRoundPlayers.Values.ToList();
            
            // Déterminer le gagnant du round (celui avec le plus de kills ou score)
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

        private bool IsGameOver()
        {
            return GameService.CurrentGameMode.IsGameOver(out _);
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
            
            // Exporter vers Excel
            ExportToExcel();
        }

        private void ExportToExcel()
        {
            // TODO: Implémenter l'export Excel
            Debug.Log($"Exporting game data: {_gameData.gameId}");
            
            string json = JsonUtility.ToJson(_gameData, true);
            Debug.Log(json);
            
            // Sauvegarder temporairement en JSON
            string path = System.IO.Path.Combine(Application.persistentDataPath, 
                $"GameData_{_gameData.gameId}.json");
            System.IO.File.WriteAllText(path, json);
            Debug.Log($"Data saved to: {path}");
        }

        public bool IsInitialized { get; set; }
        
        public void Dispose()
        {
            // Unregister tous les events
            EventBus<TriggerShootBombshell>.Deregister(_triggerShootBombshellBinding);
            EventBus<TriggerHit>.Deregister(_triggerHitBinding);
            EventBus<TriggerHealthChanged>.Deregister(_triggerHealthChangedBinding);
            EventBus<TriggerStrike>.Deregister(_triggerStrikeBinding);
            EventBus<TriggerStrikeHit>.Deregister(_triggerStrikeHitBinding);
            EventBus<EventPlayerDeath>.Deregister(_triggerDeathBinding);
            EventBus<TriggerEndRound>.Deregister(_triggerEndRoundBinding);
            
            // Sauvegarder les données avant de disposer
            if (_gameData != null && _gameData.roundsPlayed > 0)
            {
                ExportToExcel();
            }
        }
    }
}