using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace MortierFu
{
    public abstract class GameModeBase : IGameMode
    {
        protected List<PlayerTeam> teams;
        public ReadOnlyCollection<PlayerTeam> Teams { get; private set; }

        protected List<PlayerCharacter> alivePlayers;
        public ReadOnlyCollection<PlayerCharacter> AlivePlayers;
        
        protected int currentRound;
        protected int currentRank;
        protected bool oneTeamStanding;
        protected GameState currentState;

        // Dependencies
        protected LobbyService lobbyService;
        protected SceneService sceneService;
        protected AugmentSelectionSystem augmentSelectionSys;
        protected LevelSystem levelSystem;
        protected BombshellSystem bombshellSys;
        protected CameraSystem cameraSystem;
        protected CountdownTimer timer;

        protected ACT_Storm _stormInstance;

        private SO_GameModeData _gameModeData;
        private SO_StormSettings _stormSettings;

        public virtual int MinPlayerCount => _gameModeData.MinPlayerCount;
        public virtual int MaxPlayerCount => _gameModeData.MaxPlayerCount;

        public bool IsReady
        {
            get
            {
                var players = lobbyService.GetPlayers();
                return players.Count >= MinPlayerCount && players.Count <= MaxPlayerCount;
            }
        }

        public GameState CurrentState => currentState;
        public int CurrentRoundCount => currentRound;
        public float CountdownRemainingTime => timer.CurrentTime;

        /// EVENTS
        public event Action<GameState> OnGameStateChanged;
        public event Action<PlayerManager, PlayerManager> OnPlayerKilled; // (killer, victim)
        public event Action OnGameStarted;
        public event Action<int> OnRoundStarted;
        public event Action<int> OnRoundEnded;

        private const string k_gameplayActionMap = "Gameplay";
        private const string k_uiActionMap = "UI";

        public virtual async UniTask StartGame()
        {
            augmentSelectionSys = SystemManager.Instance.Get<AugmentSelectionSystem>();
            bombshellSys = SystemManager.Instance.Get<BombshellSystem>();
            cameraSystem = SystemManager.Instance.Get<CameraSystem>();
            levelSystem = SystemManager.Instance.Get<LevelSystem>();

            teams = new List<PlayerTeam>();
            Teams = teams.AsReadOnly();

            alivePlayers = new List<PlayerCharacter>();
            AlivePlayers = new ReadOnlyCollection<PlayerCharacter>(alivePlayers);

            var players = lobbyService.GetPlayers();

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                player.SpawnInGame(Vector3.zero);
                player.Character.Health.OnDeath += source => {
                    player.Metrics.TotalDeaths++;
                    alivePlayers.Remove(player.Character);
                    cameraSystem.Controller.RemoveTarget(player.transform);
                    
                    if (source is PlayerCharacter killer)
                    {
                        OnPlayerKill(killer, player.Character);
                    }
                    
                    var victimTeam = teams.FirstOrDefault(t => t.Members.Contains(player));
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
                };

                var team = new PlayerTeam(i, player);
                teams.Add(team);
            }

            if (!IsReady)
            {
                Logs.LogWarning("Not enough players or too many players for this gamemode ! Falling back to playground.");
                await levelSystem.LoadArenaMap();
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
                ResetPlayers();
                await levelSystem.LoadRaceMap();

                UpdateGameState(GameState.RaceInProgress);
                StartRace();

                var augmentPickers = GetAugmentPickers();
                await augmentSelectionSys.HandleAugmentSelection(augmentPickers, _gameModeData.AugmentSelectionDuration);

                while (!augmentSelectionSys.IsSelectionOver)
                    await UniTask.Yield();

                augmentSelectionSys.EndRace();
                EndRace();
                
                await levelSystem.LoadArenaMap();

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
                if (team.Rank == 1) continue;

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

            foreach (var team in teams.OrderByDescending(t => t.Rank))
            {
                foreach (var member in team.Members)
                {
                    var spawnPoint = levelSystem.IsRaceMap() && team.Rank == 1 ? levelSystem.GetWinnerSpawnPoint() : levelSystem.GetSpawnPoint(spawnIndex);
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
                    alivePlayers.Add(member.Character);
                }

                team.Rank = -1;
            }
            
            SpawnPlayers();
            EnablePlayerInputs(false);

            var groupMembers = alivePlayers.Select(p => p.transform).ToArray();
            cameraSystem.Controller.PopulateTargetGroup(groupMembers);
            
            HandleCountdown();
        }

        protected void HandleCountdown()
        {
            float duration = _gameModeData.RoundStartCountdown;
            #if UNITY_EDITOR
            duration *= 0.25f;
            #endif

            timer.Reset(duration - 0.01f);
            timer.OnTimerStop += HandleEndOfCountdown;
            timer.Start();
        }

        protected void HandleEndOfCountdown()
        {
            timer.OnTimerStop -= HandleEndOfCountdown;
            EnablePlayerInputs();
            PlayerCharacter.AllowGameplayActions = true;
            
            OnRoundStarted?.Invoke(currentRound);
            Logs.Log($"Round #{currentRound} is starting...");
            
            timer.Reset(_gameModeData.StormSpawnTime);
            timer.OnTimerStop += SpawnStorm;
            timer.Start();
        }

        protected virtual void EndRound()
        {
            timer.OnTimerStop -= SpawnStorm;
            timer.Stop();
            
            bombshellSys.ClearActiveBombshells();

            if (_stormInstance)
            { 
                _stormInstance.Stop();
            }
            
            ResetPlayers();
            EventBus<TriggerEndRound>.Raise(new TriggerEndRound());
            PlayerCharacter.AllowGameplayActions = false;
            EnablePlayerInputs(false);
            alivePlayers.Clear();
            
            EvaluateScores();

            OnRoundEnded?.Invoke(currentRound);
            Logs.Log("Round ended.");
        }

        private void SpawnStorm() => SpawnStormTask().Forget();
        
        protected virtual async UniTaskVoid SpawnStormTask()
        {
            var stormPrefab = await AddressablesUtils.LazyLoadAsset(_stormSettings.StormPrefab);
            if (stormPrefab == null) return;
            
            var stormGO = Object.Instantiate(stormPrefab);
            _stormInstance = stormGO.GetComponent<ACT_Storm>();
            _stormInstance.Initialize(Vector3.zero, _stormSettings);
            
            Logs.Log("Storm instantiated !");
        }
        
        protected virtual void EvaluateScores()
        {
            foreach (var team in teams)
            {
                int rankBonusScore = GetScorePerRank(team.Rank);
                int killBonusScore = team.Members.Sum(m => m.Metrics.RoundKills * _gameModeData.KillBonusScore);
                team.Score += rankBonusScore + killBonusScore;
            }
        }

        protected virtual int GetScorePerRank(int teamRank)
        {
            if (teamRank >= teams.Count) return 0;

            return teamRank switch
            {
                1 => _gameModeData.FirstRankBonusScore,
                2 => _gameModeData.SecondRankBonusScore,
                3 => _gameModeData.ThirdRankBonusScore,
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

        protected virtual void StartRace()
        {
            UpdateGameState(GameState.RaceInProgress);
            
            cameraSystem.Controller.ClearTargetGroupMember();
            cameraSystem.Controller.ResetCameraInstant();
            // Faire une fonction pour placer la cam et qu'elle ne bouge plus.
            SpawnPlayers();

            // Hide previous showcase UI            
            EnablePlayerInputs(false);

            Logs.Log("Starting augment selection...");
        }

        protected virtual void EndRace()
        {
            UpdateGameState(GameState.EndingRace);

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
            sceneService = ServiceManager.Instance.Get<SceneService>();

            // Load configuration
            var dataHandle = Addressables.LoadAssetAsync<SO_GameModeData>("DA_GM_FFA");
            await dataHandle;

            if (dataHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Logs.LogError($"[{GetType().Name}]: Failed to load the game mode data ! Issues {dataHandle.OperationException.Message}");
                return;
            }

            _gameModeData = dataHandle.Result;
            Addressables.Release(dataHandle);

            // Load Storm settings
            var stormSettingsRef = SystemManager.Config.StormSettings;
            _stormSettings = await AddressablesUtils.LazyLoadAsset(stormSettingsRef);
            if (_stormSettings == null) return;
            
            timer = new CountdownTimer(0f);

            Logs.Log("Game mode initialized successfully.");
        }

        public virtual void Update()
        { }

        public virtual void Dispose()
        {
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
                if (team.Score > _gameModeData.ScoreToWin)
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

            OnPlayerKilled?.Invoke(killer, victim);
        }
    }
}