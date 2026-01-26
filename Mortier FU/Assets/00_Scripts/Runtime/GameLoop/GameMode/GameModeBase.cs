using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Cysharp.Threading.Tasks;
using MortierFu.Analytics;
using MortierFu.Shared;
using PrimeTween;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Random = UnityEngine.Random;
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

        // protected PuddleSystem puddleSys;
        protected CameraSystem cameraSystem;
        protected CountdownTimer timer;

        private AsyncOperationHandle<SO_GameModeData> _dataHandle;
        public SO_GameModeData Data => _dataHandle.Result;

        private EventBinding<EventPlayerDeath> _playerDeathBinding;

        public virtual int MinPlayerCount => Data.MinPlayerCount;
        public virtual int MaxPlayerCount => Data.MaxPlayerCount;

        public int ScoreToWin { get; private set; }

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
        public event Func<RoundInfo, UniTask> OnRoundEndedAsync;
        public event Action OnRaceStart;
        public event Func<UniTask> OnRaceEndedUI;
        public event Action<int> OnGameEnded;

        public virtual async UniTask Initialize()
        {
            // Resolve Dependencies
            lobbyService = ServiceManager.Instance.Get<LobbyService>();
            sceneService = ServiceManager.Instance.Get<SceneService>();

            // Load configuration
            _dataHandle = await AddressablesUtils.LazyLoadAsset<SO_GameModeData>("DA_GM_FFA");

            timer = new CountdownTimer(0f);

            _playerDeathBinding = new EventBinding<EventPlayerDeath>(OnPlayerDeath);

            Logs.Log("Game mode initialized successfully.");
        }

        public virtual async UniTask StartGame()
        {
            augmentSelectionSys = SystemManager.Instance.Get<AugmentSelectionSystem>();
            cameraSystem = SystemManager.Instance.Get<CameraSystem>();
            bombshellSys = SystemManager.Instance.Get<BombshellSystem>();
            levelSystem = SystemManager.Instance.Get<LevelSystem>();

            teams = new List<PlayerTeam>();
            Teams = teams.AsReadOnly();

            alivePlayers = new List<PlayerCharacter>();
            AlivePlayers = new ReadOnlyCollection<PlayerCharacter>(alivePlayers);

            var players = lobbyService.GetPlayers();

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                player.SpawnInGame(new Vector3(i, 5, i) * 2f, player.transform.rotation);

                // Use event bus to prevent closure and weird on Death subscriptions
                // player.Character.Health. += source => OnDeath(player, source);

                var team = new PlayerTeam(i, player);
                teams.Add(team);
            }

            if (!IsReady)
            {
                Logs.LogWarning(
                    "Not enough players or too many players for this gamemode ! Falling back to playground.");
                await levelSystem.LoadArenaMap();
                StartRound();
                return;
            }

            _currentRound = new RoundInfo();
            gameVictor = null;

            GameplayCoroutine().Forget();
            Logs.Log("Starting the game...");
        }

        private TransitionColor GetTransitionColor()
        {
            if (_currentRound.WinningTeam == null || _currentRound.WinningTeam.Members.Count <= 0)
                return (TransitionColor)Random.Range(0, Enum.GetNames(typeof(TransitionColor)).Length);
            
            return _currentRound.WinningTeam.Members[0].PlayerIndex switch
            {
                0 => TransitionColor.Blue,
                1 => TransitionColor.Red,
                2 => TransitionColor.Green,
                _ => TransitionColor.Yellow
            };
        }

        protected async UniTaskVoid GameplayCoroutine()
        {
            UpdateGameState(GameState.StartGame);
            OnGameStarted?.Invoke();

            ServiceManager.Instance.Get<AudioService>().StartMusic(AudioService.FMODEvents.MUS_Gameplay).Forget();

            while (currentState != GameState.EndGame)
            {
                EnablePlayerGravity(false);

                var transitionColor = GetTransitionColor();
                await levelSystem.LoadRaceMap(true, transitionColor);
                ServiceManager.Instance.Get<AudioService>().SetPhase(1);

                UpdateGameState(GameState.DisplayAugment);
                StartRace();

                await cameraSystem.Controller.ApplyCameraMapConfigAsync(maxWaitSeconds: 4f);

                var augmentPickers = GetAugmentPickers();
                await augmentSelectionSys.HandleAugmentSelection(augmentPickers, Data.AugmentSelectionDuration);
                ServiceManager.Instance.Get<AudioService>().SetPhase(0);

                UpdateGameState(GameState.RaceInProgress);

                while (!augmentSelectionSys.IsSelectionOver)
                    await UniTask.Yield();

                augmentSelectionSys.EndRace();
                EndRace();

                EnablePlayerGravity(false);

                // TODO: Potentiellement horrible 
                if (OnRaceEndedUI != null)
                {
                    foreach (var @delegate in OnRaceEndedUI.GetInvocationList())
                    {
                        var handler = (Func<UniTask>)@delegate;
                        await handler.Invoke();
                    }
                }
                
                await levelSystem.LoadArenaMap(true, transitionColor);

                StartRound();

                while (!oneTeamStanding)
                    await UniTask.Yield();

                UpdateGameState(GameState.EndRound);
                EndRound();

                // TODO: Potentiellement horrible 
                if (OnRoundEndedAsync != null)
                {
                    foreach (var @delegate in OnRoundEndedAsync.GetInvocationList())
                    {
                        var handler = (Func<RoundInfo, UniTask>)@delegate;
                        await handler.Invoke(_currentRound);
                    }
                }

                UpdateGameState(GameState.DisplayScores);
                //DisplayScores();

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

        private void EnablePlayerGravity(bool enabled = true)
        {
            foreach (var team in teams)
            {
                foreach (var member in team.Members)
                {
                    member.Character.Controller.rigidbody.useGravity = enabled;
                }
            }
        }

        private void SpawnPlayers()
        {
            bool opposite = _currentRound.RoundIndex % 2 == 0;
            int spawnIndex = opposite ? teams.Sum(t => t.Members.Count()) - 1 : 0;

            foreach (var team in teams.OrderByDescending(t => t.Rank))
            {
                foreach (var member in team.Members)
                {
                    Transform spawnPoint;

                    if (levelSystem.IsRaceMap())
                    {
                        spawnPoint = team.Rank == 1
                            ? levelSystem.GetWinnerSpawnPoint()
                            : levelSystem.GetSpawnPoint(spawnIndex);
                    }
                    else
                    {
                        spawnPoint = levelSystem.GetSpawnPoint(spawnIndex);
                    }

                    member.SpawnInGame(spawnPoint.position, spawnPoint.rotation);

                    if (opposite)
                        spawnIndex--;
                    else
                        spawnIndex++;
                }
            }
        }

        private void SpawnWinnerTeam(PlayerTeam winnerTeam)
        {
            var spawnPoint = levelSystem.GetRoundWinnerSpawnPoint();

            foreach (var member in winnerTeam.Members)
            {
                member.SpawnInGame(spawnPoint.position, spawnPoint.rotation);
            }
        }

        public void EnablePlayerInputs(bool enable = true)
        {
            foreach (var team in teams)
            {
                foreach (var member in team.Members)
                {
                    member.EnableGameplayInputMap(enable);
                }
            }

#if UNITY_EDITOR
            if (EditorPrefs.GetBool("DummyDebugToolEnabled", true))
            {
                PlayerInputSwapper.Instance.UpdateActivePlayer();
            }
#endif
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
            EnablePlayerGravity();
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

        protected void HandleEndOfCountdown()
        {
            timer.OnTimerStop -= HandleEndOfCountdown;

            EnablePlayerInputs();
            PlayerCharacter.AllowGameplayActions = true;

            Logs.Log($"Round #{_currentRound.RoundIndex} is starting...");
        }

        protected void HandleStartOfCountdown()
        {
            OnRoundStarted?.Invoke(_currentRound);
        }

        protected virtual void EndRound()
        {
            timer.Stop();

            EventBus<EventPlayerDeath>.Deregister(_playerDeathBinding);

            bombshellSys.ClearActiveBombshells();

            EventBus<TriggerEndRound>.Raise(new TriggerEndRound());
            EnablePlayerInputs(false);
            PlayerCharacter.AllowGameplayActions = false;
            alivePlayers.Clear();

            EvaluateScores();

            _currentRound.WinningTeam = teams.FirstOrDefault(t => t.Rank == 1);

            if (_currentRound.WinningTeam != null)
            {
                var winner = _currentRound.WinningTeam.Members[0];
                winner.Character.Reset();

                SpawnWinnerTeam(_currentRound.WinningTeam);

                cameraSystem.Controller.EndFightCameraMovement(
                    winner.Character.transform,
                    2f);

                winner.Character.WinRoundDance();
            }

            OnRoundEnded?.Invoke(_currentRound);
        }

        protected virtual void EvaluateScores()
        {
            foreach (var team in teams)
            {
                if (team.Score >= ScoreToWin)
                {
                    if (team.Rank != 1) continue;
                    gameVictor = team;
                }
                else
                {
                    int rankBonusScore = GetScorePerRank(team.Rank);

                    int killBonusScore = 0;
                    foreach (var member in team.Members)
                    {
                        foreach (var deathCause in member.Metrics.RoundKills)
                        {
                            killBonusScore += Data.KillBonusScore;
                            switch (deathCause)
                            {
                                case E_DeathCause.Fall:
                                    killBonusScore += Data.KillPushBonusScore;
                                    break;
                                case E_DeathCause.VehicleCrash:
                                    killBonusScore += Data.KillCarCrashBonusScore;
                                    break;
                            }
                        }
                    }

                    team.Score = Math.Min(team.Score + rankBonusScore + killBonusScore, ScoreToWin);

                    // notify analytics system
                    var analyticsSys = SystemManager.Instance.Get<AnalyticsSystem>();
                    analyticsSys?.OnScoreChanged(team.Members[0].Character, team.Score);
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

            ResetPlayers();
            SpawnPlayers();
            EnablePlayerGravity();

            // Hide previous showcase UI            
            EnablePlayerInputs(false);
            PlayerCharacter.AllowGameplayActions = false;

            OnRaceStart?.Invoke();

            Logs.Log("Starting augment selection...");
        }

        protected virtual void EndRace()
        {
            UpdateGameState(GameState.EndingRace);

            EnablePlayerInputs(false);

            // stop selection UI

            ResetPlayers();
            EventBus<TriggerEndRound>.Raise(new TriggerEndRound());
        }

        public virtual void EndGame()
        {
            ServiceManager.Instance.Get<AudioService>().StartMusic(AudioService.FMODEvents.MUS_Victory).Forget();

            foreach (var team in teams)
            {
                foreach (var member in team.Members)
                {
                    member.EnableGameplayInputMap(false);
                }
            }
            
            OnGameEnded?.Invoke(GetWinnerPlayerIndex());
            Logs.Log("Game has ended.");
        }

        public void ReturnToMainMenu()
        {
            ReturnToMainMenuAfterDelay().Forget();
        }

        private async UniTaskVoid ReturnToMainMenuAfterDelay()
        {
            SystemManager.Instance.Dispose();

            lobbyService.ClearPlayers();

            await levelSystem.UnloadCurrentMap();
            await sceneService.UnloadScene("Gameplay");

            await sceneService.LoadScene("MainMenu", true);
            //TODO: a full check au cas ou
        }

        protected virtual void UpdateGameState(GameState newState)
        {
            currentState = newState;
            OnGameStateChanged?.Invoke(newState);
        }

        public void SetScoreToWin(int score)
        {
            ScoreToWin = score;
        }

        public virtual void Update()
        {
        }

        public virtual void Dispose()
        {
            Addressables.Release(_dataHandle);

            EventBus<EventPlayerDeath>.Deregister(_playerDeathBinding);
            _playerDeathBinding = null;

            teams.Clear();
            timer.Dispose();
        }

        protected void OnPlayerDeath(EventPlayerDeath evt)
        {
            var player = evt.Character.Owner;

            player.Metrics.TotalDeaths++;
            alivePlayers.Remove(player.Character);
            cameraSystem.Controller.RemoveTarget(evt.Character.transform);

            if (evt.Context.Killer)
            {
                OnPlayerKill(evt);
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

        protected virtual void OnPlayerKill(EventPlayerDeath evt)
        {
            var killer = evt.Context.Killer.Owner;
            var victim = evt.Character.Owner;

            if (killer != victim)
            {
                killer.Metrics.RoundKills.Add(evt.Context.DeathCause);
            }

            OnPlayerKilled?.Invoke(killer, victim);
        }

        public int GetWinnerPlayerIndex()
        {
            if (IsGameOver(out var victor))
            {
                return victor?.Index ?? -1;
            }

            return _currentRound.WinningTeam.Index; // Aucun gagnant pour l'instant
        }
    }
}