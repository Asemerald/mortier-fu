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

        protected PlayerTeam gameVictor;
        protected GameState currentState;

        private ScoreController _scoreController;

        private RoundController _roundController;

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

            _roundController = new RoundController(teams, alivePlayers);
            _roundController.OnPlayerDied += HandleRoundPlayerDied;
            _roundController.OnPlayerKilled += HandleRoundPlayerKilled;

            AlivePlayers = _roundController.AlivePlayers;

            _scoreController = new ScoreController(
                Data,
                teams,
                ScoreToWin,
                SystemManager.Instance.Get<AnalyticsSystem>()
            );

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

                while (_roundController != null && !_roundController.OneTeamStanding)
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
            if (_scoreController == null)
            {
                victor = null;
                return false;
            }

            return _scoreController.IsGameOver(out victor);
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

        public void SetPlayerControlContext(PlayerControlContext context)
        {
            foreach (var team in teams)
            {
                foreach (var member in team.Members)
                {
                    member.SetControlContext(context);
                }
            }

#if UNITY_EDITOR
            if (EditorPrefs.GetBool("DummyDebugToolEnabled", true))
            {
                PlayerInputSwapper.Instance.UpdateActivePlayer();
            }
#endif
        }

        public void EnablePlayerInputs(bool enable = true)
        {
            SetPlayerControlContext(enable
                ? PlayerControlContext.RoundGameplay
                : PlayerControlContext.Scoreboard);
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

            ResetPlayers();
            SpawnPlayers();
            EnablePlayerGravity();

            SetPlayerControlContext(PlayerControlContext.RoundCountdown);

            _roundController.BeginRound();

            var groupMembers = AlivePlayers.Select(player => player.transform).ToArray();
            cameraSystem.Controller.PopulateTargetGroup(groupMembers);

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

            SetPlayerControlContext(PlayerControlContext.RoundGameplay);

            Logs.Log($"Round #{_currentRound.RoundIndex} is starting...");
        }

        protected void HandleStartOfCountdown()
        {
            OnRoundStarted?.Invoke(_currentRound);
        }

        protected virtual void EndRound()
        {
            timer.Stop();

            _roundController.EndRound();

            bombshellSys.ClearActiveBombshells();

            EventBus<TriggerEndRound>.Raise(new TriggerEndRound());

            SetPlayerControlContext(PlayerControlContext.RoundEnded);

            EvaluateScores();

            _currentRound.WinningTeam = _roundController.WinningTeam;

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
            if (_scoreController == null)
            {
                Logs.LogError("[GameModeBase] Cannot evaluate scores because ScoreController is null.");
                return;
            }

            gameVictor = _scoreController.EvaluateScores();
        }

        public int GetScorePerRank(int teamRank)
        {
            return _scoreController?.GetScorePerRank(teamRank) ?? 0;
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
            SetPlayerControlContext(PlayerControlContext.AugmentShowcase);

            OnRaceStart?.Invoke();

            Logs.Log("Starting augment selection...");
        }

        protected virtual void EndRace()
        {
            UpdateGameState(GameState.EndingRace);

            SetPlayerControlContext(PlayerControlContext.RoundEnded);

            // stop selection UI

            ResetPlayers();
            EventBus<TriggerEndRound>.Raise(new TriggerEndRound());
        }
        
        public int GetWinnerPlayerIndex()
        {
            if (IsGameOver(out var victor))
            {
                return victor?.Index ?? -1;
            }

            if (_currentRound.WinningTeam != null)
            {
                return _currentRound.WinningTeam.Index;
            }

            Logs.LogWarning("[GameModeBase] GetWinnerPlayerIndex called but no game victor or round winner was found.");
            return -1;
        }

        public virtual void EndGame()
        {
            ServiceManager.Instance.Get<AudioService>().StartMusic(AudioService.FMODEvents.MUS_Victory).Forget();

            SetPlayerControlContext(PlayerControlContext.EndGame);
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
            _scoreController?.SetScoreToWin(score);
        }

        public virtual void Update()
        {
        }

        public virtual void Dispose()
        {
            Addressables.Release(_dataHandle);

            if (_roundController != null)
            {
                _roundController.OnPlayerDied -= HandleRoundPlayerDied;
                _roundController.OnPlayerKilled -= HandleRoundPlayerKilled;
                _roundController.Dispose();
                _roundController = null;
            }

            _scoreController = null;

            teams.Clear();
            timer.Dispose();
        }
        
        private void HandleRoundPlayerDied(PlayerCharacter character)
        {
            if (character == null)
                return;

            cameraSystem?.Controller?.RemoveTarget(character.transform);
        }

        private void HandleRoundPlayerKilled(PlayerManager killer, PlayerManager victim)
        {
            OnPlayerKilled?.Invoke(killer, victim);
        }
    }
}