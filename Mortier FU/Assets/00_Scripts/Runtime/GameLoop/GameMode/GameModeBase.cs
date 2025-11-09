using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using Random = UnityEngine.Random;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace MortierFu
{
    public abstract class GameModeBase : IGameMode {
        protected List<PlayerTeam> teams;
        public ReadOnlyCollection<PlayerTeam> Teams { get; private set; }

        protected int currentRound;
        protected int currentRank;
        protected bool oneTeamStanding;
        protected GameState currentState;

        // Dependencies
        protected LobbyService lobbyService;
        protected AugmentSelectionSystem augmentSelectionSys;
        protected LevelSystem levelSystem;
        protected BombshellSystem bombshellSys;
        protected CountdownTimer timer;

        private AsyncOperationHandle<SO_GameModeData> _gameModeDataHandle;

        public virtual SO_GameModeData GameModeData => _gameModeDataHandle.Result;
        public virtual int MinPlayerCount => GameModeData.MinPlayerCount;
        public virtual int MaxPlayerCount => GameModeData.MaxPlayerCount;

        public bool IsReady {
            get {
                var players = lobbyService.GetPlayers();
                return players.Count >= MinPlayerCount && players.Count <= MaxPlayerCount;
            }
        }

        public int CurrentRoundCount => currentRound;
        public GameState CurrentState => currentState;
        
        /// EVENTS
        public event Action<GameState> OnGameStateChanged;
        public event Action<PlayerManager, PlayerManager> OnPlayerKilled; // (killer, victim)
        public event Action OnGameStarted;
        public event Action<int> OnRoundStarted;
        public event Action<int> OnRoundEnded;

        private const string k_gameplayActionMap = "Gameplay";
        private const string k_uiActionMap = "UI";

        public float CountdownRemainingTime => timer.CurrentTime;
        
        public virtual async UniTask StartGame()
        {
            augmentSelectionSys = SystemManager.Instance.Get<AugmentSelectionSystem>();
            bombshellSys = SystemManager.Instance.Get<BombshellSystem>();
            levelSystem = SystemManager.Instance.Get<LevelSystem>();
            
            // TODO: Move scene loading to its own service and load and unload maps each round
            var mapHandle = SceneManager.LoadSceneAsync("Map 01", LoadSceneMode.Additive);
            await mapHandle.ToUniTask();
            
            teams = new List<PlayerTeam>();
            Teams = teams.AsReadOnly();
            
            var players = lobbyService.GetPlayers();
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                player.SpawnInGame(Vector3.zero);
                player.Character.Health.OnDeath += source =>
                {
                    player.Metrics.TotalDeaths++;
                    
                    if (source is PlayerCharacter killer)
                    {
                        OnPlayerKill(killer, player.Character);
                    }
                };
                
                var team = new PlayerTeam(i, player);
                teams.Add(team);
            }
            
            if (!IsReady) {
                Logs.LogWarning("Not enough players or too many players for this gamemode ! Falling back to playground.");
                StartRound();
                return;
            }
            
            currentRound = 0;

            GameplayCoroutine().Forget();
            Logs.Log("Starting the game...");
        }
        
        // TODO: Maybe it is valuable to ask for player 1 input to proceed to each step for fluidity
        protected async UniTaskVoid GameplayCoroutine()
        {
            UpdateGameState(GameState.StartGame);
            OnGameStarted?.Invoke();

            while (currentState != GameState.EndGame)
            {
                UpdateGameState(GameState.AugmentSelection);
                StartAugmentSelection();
                
                var augmentPickers = GetAugmentPickers();
                await augmentSelectionSys.HandleAugmentSelection(augmentPickers, GameModeData.AugmentSelectionDuration);

                while (!augmentSelectionSys.IsSelectionOver)
                    await UniTask.Yield();
                    
                augmentSelectionSys.EndAugmentSelection();
                EndAugmentSelection();
                
                StartRound();

                while (!oneTeamStanding)
                    await UniTask.Yield();
                
                UpdateGameState(GameState.EndRound);
                EndRound();

                UpdateGameState(GameState.DisplayScores);
                DisplayScores();
                
                HideScores();
            
                if (IsGameOver(out PlayerTeam victor))
                {
                    Logs.Log($"Game Over! Team {victor.Index} wins!");
                    UpdateGameState(GameState.EndGame);
                }
            }

            EndGame();
        }

        private List<PlayerManager> GetAugmentPickers()
        {
            var pickers = new List<PlayerManager>();
            foreach (var team in teams)
            {
                if(team.Rank == 1) continue;
                
                pickers.AddRange(team.Members);
            }

            if (pickers.Count == 0)
            {
                Logs.LogWarning("Found no pickers for this augment selection phase.");
            }
            
            return pickers;
        }

        private void SpawnPlayers()
        {
            bool opposite = currentRound % 2 == 0;
            int spawnIndex = opposite ? teams.Sum(t => t.Members.Count()) - 1 : 0;
            
            foreach (var team in teams)
            {
                foreach (var member in team.Members)
                {
                    var spawnPoint = levelSystem.GetSpawnPoint(spawnIndex);
                    member.SpawnInGame(spawnPoint.position);
                    member.Character.transform.position = spawnPoint.position;
                    if (opposite)
                        spawnIndex--;
                    else 
                        spawnIndex++;
                }
            }
        }

        public void EnablePlayerInputs(bool enabled = true)
        {
            foreach (var team in teams)
            {
                foreach (var member in team.Members)
                {
                    member.PlayerInput.SwitchCurrentActionMap(enabled ? k_gameplayActionMap : k_uiActionMap); // Utiliser ça et faire un helper qu'on met ici pour vérifier
                    // si c'est joueur 0 ou pas.
                }
            }
        }
        
        protected virtual bool AllPlayersReady()
        {
            // TODO: Implement player ready check
            return false;
        }

        protected virtual void ResetPlayers()
        {
            foreach (var team in teams)
            {
                foreach (var member in team.Members)
                {
                    member.Character.Reset();
                }
            }
        }
        
        protected virtual void StartRound()
        {
            UpdateGameState(GameState.Round);

            currentRound++;
            currentRank = teams.Count;
            oneTeamStanding = false;
            
            SpawnPlayers();
            EnablePlayerInputs(false);

            foreach (var team in teams)
            {
                foreach (var member in team.Members)
                {
                    member.Metrics.ResetRoundMetrics();
                }

                team.Rank = -1;
            }

            HandleCountdown();
            
            OnRoundStarted?.Invoke(currentRound);
            Logs.Log($"Round #{currentRound} is starting...");
        }

        protected void HandleCountdown()
        {
            timer.Reset(GameModeData.RoundStartCountdown - 0.01f);
            timer.OnTimerStop += HandleEndOfCountdown;
            timer.Start();
        }
        
        protected void HandleEndOfCountdown()
        {
            timer.OnTimerStop -= HandleEndOfCountdown;
            EnablePlayerInputs();
            PlayerCharacter.AllowGameplayActions = true;
        }


        protected virtual void EndRound()
        {
            timer.Stop();
            bombshellSys.ClearActiveBombshells();
            ResetPlayers();
            PlayerCharacter.AllowGameplayActions = false;
            EnablePlayerInputs(false);
            
            EvaluateScores();
            
            OnRoundEnded?.Invoke(currentRound);
            Logs.Log("Round ended.");
        }
        
        protected virtual void EvaluateScores()
        {
            foreach (var team in teams)
            {
                int rankBonusScore = GetScorePerRank(team.Rank);
                int killBonusScore = team.Members.Sum(m => m.Metrics.RoundKills * GameModeData.KillBonusScore);
                team.Score += rankBonusScore + killBonusScore;
            }
        }
        
        protected virtual  int GetScorePerRank(int teamRank)
        {
            if (teamRank >= teams.Count) return 0;

            return teamRank switch
            {
                1 => GameModeData.FirstRankBonusScore,
                2 => GameModeData.SecondRankBonusScore,
                3 => GameModeData.ThirdRankBonusScore,
                _ => 0
            };
        }

        protected virtual void DisplayScores()
        {
            UpdateGameState(GameState.DisplayScores);
            
            // Update UI Score Panel
            // Link countdown timer
            
            Logs.Log("Displaying scores...");
            
            foreach (var team in teams)
            {
                Logs.Log($"Team Score: {team.Score}");
            }
        }

        protected virtual void HideScores()
        {
            timer.Stop();
            
            // Hide UI
        }
        
        protected virtual void StartAugmentSelection()
        {
            UpdateGameState(GameState.AugmentSelection);
            
            foreach (var team in teams)
            {
                foreach (var member in team.Members)
                {
                    member.Character.transform.position = Vector3.up * 3f;
                }
            }
            
            // Hide previous showcase UI            
            EnablePlayerInputs(false);
            
            Logs.Log("Starting augment selection...");
        }

        protected virtual void EndAugmentSelection()
        {
            UpdateGameState(GameState.EndAugmentSelection);

            EnablePlayerInputs(false);

            // stop selection UI
            
            ResetPlayers();
        }

        protected virtual void EndGame()
        {
            // The game state is already set to EndGame at that point
            // Show the victory screen
        }
        
        protected virtual void UpdateGameState(GameState newState)
        {
            currentState = newState;
            OnGameStateChanged?.Invoke(newState);
        }
        
        public virtual async UniTask Initialize()
        {
            // Resolve Dependencies
            lobbyService = ServiceManager.Instance.Get<LobbyService>();
            
            // Load configuration
            _gameModeDataHandle = Addressables.LoadAssetAsync<SO_GameModeData>("DA_GM_FFA");
            await _gameModeDataHandle;
            
            timer = new CountdownTimer(0f);
            
            Logs.Log("Game mode initialized successfully.");
        }

        public virtual void Update()
        { }
        
        public virtual void Dispose()
        {
            Addressables.Release(_gameModeDataHandle);
            
            teams.Clear();
            timer.Dispose();
        }

        /// <summary>
        /// Default implementation is to select the victor based on a score to win variable.
        /// </summary>
        /// <param name="victor">The team which won the game.</param>
        /// <returns>If the game is over.</returns>
        public virtual bool IsGameOver(out PlayerTeam victor)
        {
            foreach (var team in teams)
            {
                if (team.Score > GameModeData.ScoreToWin)
                {
                    victor = team;
                    return true;
                }
            }

            victor = null;
            return false;
        }

        public virtual void OnPlayerKill(PlayerCharacter killerCharacter, PlayerCharacter victimCharacter)
        {
            var killer = killerCharacter.Owner;
            var victim = victimCharacter.Owner;

            if (killer != victim)
            {
                killer.Metrics.RoundKills += 1;
            }

            // TODO: Can be improved
            var victimTeam = teams.FirstOrDefault(t => t.Members.Contains(victim));
            if (victimTeam == null)
            {
                Logs.LogError("[GameModeBase] Victim's team not found!");
                return;
            }

            if (victimTeam.Members.All(m => m.Character.Health.IsAlive == false))
            {
                victimTeam.Rank = currentRank;
                currentRank--;
            }

            // Check if there is one team standing
            int aliveTeamIndex = -1;
            for (int i = 0; i < teams.Count; i++)
            {
                PlayerTeam team = teams[i];
                if (team.Members.Any(m => m.Character.Health.IsAlive))
                {
                    if (aliveTeamIndex == -1)
                    {
                        aliveTeamIndex = i;
                    }
                    else
                    {
                        aliveTeamIndex = -1;
                        break;
                    }
                }
            }

            // Set the rank of the winning team to 1 if one.
            if (aliveTeamIndex != -1)
            {
                teams[aliveTeamIndex].Rank = 1;
                oneTeamStanding = true;
            }

            OnPlayerKilled?.Invoke(killer, victim);
        }
    }
}