using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;
using Vector3 = UnityEngine.Vector3;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MortierFu
{
    // TODO: Le GameMode de salopard, il fait bientôt 600 lignes, il faudrait peut-être mettre le bébé au régime
    public abstract class GameModeBase : IGameMode
    {
        protected List<PlayerTeam> teams;
        public List<RoundInfo> RoundHistory { get; protected set; } = new List<RoundInfo>();
        private RoundInfo _currentRound;
        public ReadOnlyCollection<PlayerTeam> Teams { get; private set; }

        protected List<PlayerCharacter> alivePlayers;
        public ReadOnlyCollection<PlayerCharacter> AlivePlayers;
        
        protected int currentRank;
        protected bool oneTeamStanding;
        protected GameState currentState;
        protected PlayerTeam gameVictor;

        // Dependencies
        protected LobbyService lobbyService;
        protected SceneService sceneService;
        protected AugmentSelectionSystem augmentSelectionSys;
        protected LevelSystem levelSystem;
        protected BombshellSystem bombshellSys;
        protected PuddleSystem puddleSys;
        protected CameraSystem cameraSystem;
        protected CountdownTimer timer;

        protected ACT_Storm stormInstance;

        private AsyncOperationHandle<SO_StormSettings> _stormSettingsHandle;
        public SO_StormSettings StormSettings => _stormSettingsHandle.Result;
            
        private AsyncOperationHandle<SO_GameModeData> _dataHandle;
        public SO_GameModeData Data => _dataHandle.Result;

        private EventBinding<EventPlayerDeath> _playerDeathBinding;
        
        public virtual int MinPlayerCount => Data.MinPlayerCount;
        public virtual int MaxPlayerCount => Data.MaxPlayerCount;

        public bool IsReady
        {
            get
            {
                var players = lobbyService.GetPlayers();
                return players.Count >= MinPlayerCount && players.Count <= MaxPlayerCount;
            }
        }

        public GameState CurrentState => currentState;
        public int CurrentRoundCount => _currentRound.RoundIndex;
        public float CountdownRemainingTime => timer.CurrentTime;

        /// EVENTS
        public event Action<GameState> OnGameStateChanged;
        
        /// <summary>
        /// Invoked when a player kills another player.
        /// <remarks>Killer / Victim</remarks>
        /// </summary>
        public event Action<PlayerManager, PlayerManager> OnPlayerKilled;
        public event Action OnGameStarted;
        public event Action<RoundInfo> OnRoundStarted;
        public event Action OnScoreDisplayOver;
        public event Action<RoundInfo> OnRoundEnded;
        
        public event Func<UniTask> OnRaceEndedUI;
        
        public event Action<int> OnGameEnded;

        private const string k_gameplayActionMap = "Gameplay";
        private const string k_uiActionMap = "UI";

        public virtual async UniTask StartGame()
        {
            augmentSelectionSys = SystemManager.Instance.Get<AugmentSelectionSystem>();
            cameraSystem = SystemManager.Instance.Get<CameraSystem>();
            bombshellSys = SystemManager.Instance.Get<BombshellSystem>();
            puddleSys = SystemManager.Instance.Get<PuddleSystem>();
            levelSystem = SystemManager.Instance.Get<LevelSystem>();

            teams = new List<PlayerTeam>();
            Teams = teams.AsReadOnly();

            alivePlayers = new List<PlayerCharacter>();
            AlivePlayers = new ReadOnlyCollection<PlayerCharacter>(alivePlayers);

            var players = lobbyService.GetPlayers();

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                player.SpawnInGame(new Vector3(i, 5, i) * 2f);
                
                // Use event bus to prevent closure and weird on Death subscriptions
                // player.Character.Health. += source => OnDeath(player, source);
                
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

            _currentRound = new RoundInfo();
            gameVictor = null;

            GameplayCoroutine().Forget();
            Logs.Log("Starting the game...");
        }

        protected async UniTaskVoid GameplayCoroutine()
        {
            UpdateGameState(GameState.StartGame);
            OnGameStarted?.Invoke();

            while (currentState != GameState.EndGame)
            {
                await levelSystem.LoadRaceMap();

                UpdateGameState(GameState.RaceInProgress);
                StartRace();
                
                var augmentPickers = GetAugmentPickers();
                await augmentSelectionSys.HandleAugmentSelection(augmentPickers, Data.AugmentSelectionDuration);

                while (!augmentSelectionSys.IsSelectionOver)
                    await UniTask.Yield();

                augmentSelectionSys.EndRace();
                EndRace();
                
                if (OnRaceEndedUI != null)
                {
                    foreach (var @delegate in OnRaceEndedUI.GetInvocationList())
                    {
                        var handler = (Func<UniTask>)@delegate;
                        await handler.Invoke();
                    }
                }
                
                await levelSystem.LoadArenaMap();

                StartRound();

                while (!oneTeamStanding)
                    await UniTask.Yield();

                UpdateGameState(GameState.EndRound);
                EndRound();

                UpdateGameState(GameState.DisplayScores);
                DisplayScores();
                
                await UniTask.Delay(TimeSpan.FromSeconds(Data.DisplayScoresDuration));
                
                HideScores();

                if (IsGameOver(out gameVictor))
                {
                    Logs.Log($"Game Over! Team {gameVictor.Index} wins!");
                    UpdateGameState(GameState.EndGame);
                }
            }

            EndGame();
        }

        public bool IsGameOver(out PlayerTeam victor)
        {
            victor = gameVictor;
            return gameVictor != null;
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
            bool opposite = _currentRound.RoundIndex % 2 == 0;
            int spawnIndex = opposite ? teams.Sum(t => t.Members.Count()) - 1 : 0;

            foreach (var team in teams.OrderByDescending(t => t.Rank))
            {
                foreach (var member in team.Members)
                {
                    var spawnPoint = levelSystem.IsRaceMap() && team.Rank == 1 ? levelSystem.GetWinnerSpawnPoint() : levelSystem.GetSpawnPoint(spawnIndex);
                    member.SpawnInGame(spawnPoint.position);
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
            
#if UNITY_EDITOR
            PlayerInputSwapper.Instance.UpdateActivePlayer();
#endif
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

            _currentRound = new RoundInfo
            {
                RoundIndex = RoundHistory.Count + 1,
                WinningTeam = teams.FirstOrDefault(t => t.Rank == 1)
            };
            
            RoundHistory.Add(_currentRound);
            
            currentRank = teams.Count;
            oneTeamStanding = false;
            
            ResetPlayers();
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

            var groupMembers = alivePlayers.Select(p => p.transform).ToArray();
            cameraSystem.Controller.PopulateTargetGroup(groupMembers);
            
            EventBus<EventPlayerDeath>.Register(_playerDeathBinding);
            
            HandleCountdown();
        }

        protected void HandleCountdown()
        {
            float duration = Data.RoundStartCountdown;
            #if UNITY_EDITOR
            int speedMult = EditorPrefs.GetInt("CountdownSpeedMult", 1);
            duration *= 1f / speedMult;
            #endif

            timer.Reset(duration - 0.01f);
            
            timer.OnTimerStart -= HandleStartOfCountdown;
            timer.OnTimerStop -= HandleEndOfCountdown;

            timer.OnTimerStart += HandleStartOfCountdown;
            timer.OnTimerStop += HandleEndOfCountdown;
            
            timer.Start();
        }

        protected async void HandleEndOfCountdown()
        {
            timer.OnTimerStop -= HandleEndOfCountdown;

            await UniTask.Delay(TimeSpan.FromSeconds(Data.RoundStartDelay));
                
            EnablePlayerInputs();
            PlayerCharacter.AllowGameplayActions = true;

            Logs.Log($"Round #{_currentRound.RoundIndex} is starting...");
            
            // timer.Reset(_gameModeData.StormSpawnTime);
            // timer.OnTimerStop += SpawnStorm;
            // timer.Start();
        }
        
        protected void HandleStartOfCountdown()
        {
            OnRoundStarted?.Invoke(_currentRound);
        }

        protected virtual void EndRound()
        {
            timer.OnTimerStop -= SpawnStorm;
            timer.Stop();
            
            EventBus<EventPlayerDeath>.Deregister(_playerDeathBinding);
            
            bombshellSys.ClearActiveBombshells();
            puddleSys.ClearActivePuddles();
            
            if (stormInstance)
            { 
                stormInstance.Stop();
            }
            
            ResetPlayers();
            SpawnPlayers();
            EventBus<TriggerEndRound>.Raise(new TriggerEndRound());
            PlayerCharacter.AllowGameplayActions = false;
            EnablePlayerInputs(false);
            alivePlayers.Clear();
            
            EvaluateScores();
            
            _currentRound.WinningTeam = teams.FirstOrDefault(t => t.Rank == 1);
            
            //TEMPORARY
            cameraSystem.Controller.EndFightCameraMovement(_currentRound.WinningTeam.Members[0].Character.transform);

            OnRoundEnded?.Invoke(_currentRound);
        }

        private void SpawnStorm() => SpawnStormTask().Forget();
        
        protected virtual async UniTaskVoid SpawnStormTask()
        {
            var stormGo = await StormSettings.StormPrefab.InstantiateAsync();
            stormInstance = stormGo.GetComponent<ACT_Storm>();
            stormInstance.Initialize(Vector3.zero, StormSettings);
            
            Logs.Log("Storm instantiated !");
        }
        
        protected virtual void EvaluateScores()
        {
            foreach (var team in teams)
            {
                if (team.Score >= Data.ScoreToWin)
                {
                    if (team.Rank != 1) continue;
                    gameVictor = team;
                }
                else
                {
                    int rankBonusScore = GetScorePerRank(team.Rank);
                    int killBonusScore = team.Members.Sum(m => m.Metrics.RoundKills * Data.KillBonusScore);
                    team.Score = Math.Min(team.Score + rankBonusScore + killBonusScore, Data.ScoreToWin);
                }
            }
        }
        
        public int GetScorePerRank(int teamRank)
        {
            if (teamRank >= teams.Count) return 0;

            return teamRank switch
            {
                1 => Data.FirstRankBonusScore,
                2 => Data.SecondRankBonusScore,
                3 => Data.ThirdRankBonusScore,
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
            //TEMPORARY
            cameraSystem.Controller.ResetToMainCamera();
            
            timer.Stop();

            // Hide UI
            OnScoreDisplayOver?.Invoke();
        }

        protected virtual void StartRace()
        {
            UpdateGameState(GameState.RaceInProgress);
            
            cameraSystem.Controller.ClearTargetGroupMember();

            ResetPlayers();
            SpawnPlayers();

            // Hide previous showcase UI            
            EnablePlayerInputs(false);
            PlayerCharacter.AllowGameplayActions = false;

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
            OnGameEnded?.Invoke(GetWinnerPlayerIndex());
            Logs.Log("Game has ended.");
            ReturnToMainMenuAfterDelay(5f).Forget();
        }
        
        private async UniTaskVoid ReturnToMainMenuAfterDelay(float delay)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay));
            await sceneService.LoadScene("MainMenu", true, true);
            //TODO: a full check, je pense que ATM le SystemManager est pas Dispose
            ServiceManager.Instance.Get<GameService>().Dispose();
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
            _dataHandle = await AddressablesUtils.LazyLoadAsset<SO_GameModeData>("DA_GM_FFA");

            // Load Storm settings
            _stormSettingsHandle = await SystemManager.Config.StormSettings.LazyLoadAssetRef();
            
            timer = new CountdownTimer(0f);

            _playerDeathBinding = new EventBinding<EventPlayerDeath>(OnPlayerDeath);
            
            Logs.Log("Game mode initialized successfully.");
        }

        public virtual void Update()
        { }

        public virtual void Dispose()
        {
            if (stormInstance)
            {
                Addressables.ReleaseInstance(stormInstance.gameObject);
            }
            
            Addressables.Release(_dataHandle);
            Addressables.Release(_stormSettingsHandle);

            EventBus<EventPlayerDeath>.Deregister(_playerDeathBinding);
            _playerDeathBinding = null;
            
            teams.Clear();
            timer.Dispose();
        }

        protected void OnPlayerDeath(EventPlayerDeath evt) {
            var player = evt.Character.Owner;
            
            player.Metrics.TotalDeaths++;
            alivePlayers.Remove(player.Character);
            cameraSystem.Controller.RemoveTarget(player.transform);

            if (evt.Source is PlayerCharacter killer)
            {
                OnPlayerKill(killer, evt.Character);
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
                
                Logs.Log("This eliminated, assigned rank #" + victimTeam.Rank);
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
        }

        protected virtual void OnPlayerKill(PlayerCharacter killerCharacter, PlayerCharacter victimCharacter)
        {
            var killer = killerCharacter.Owner;
            var victim = victimCharacter.Owner;

            if (killer != victim)
            {
                killer.Metrics.RoundKills += 1;
            }

            OnPlayerKilled?.Invoke(killer, victim);
        }
        
        public int GetWinnerPlayerIndex()
        {
            if (IsGameOver(out var victor))
            {
                return victor?.Index ?? -1;
            }
            
            return -1; // Aucun gagnant pour l'instant
        }

    }
}