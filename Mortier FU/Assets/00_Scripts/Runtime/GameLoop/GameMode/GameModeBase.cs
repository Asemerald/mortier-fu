using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortierFu.Analytics;
using MortierFu.Shared;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MortierFu
{
    // Orchestrates the high-level match flow.
    // Most concrete responsibilities are delegated to dedicated controllers.
    public abstract class GameModeBase : IGameMode
    {
        protected List<PlayerTeam> teams;
        public List<RoundInfo> RoundHistory { get; protected set; } = new();
        private RoundInfo _currentRound;
        public ReadOnlyCollection<PlayerTeam> Teams { get; private set; }

        protected List<PlayerCharacter> alivePlayers;
        public ReadOnlyCollection<PlayerCharacter> AlivePlayers { get; private set; }

        protected PlayerTeam gameVictor;
        protected GameState currentState;

        private ScoreController _scoreController;
        private RoundController _roundController;
        private PlayerSpawnController _playerSpawnController;
        private AugmentRaceController _augmentRaceController;
        private RoundStartController _roundStartController;
        private RoundWinnerPresentationController _roundWinnerPresentationController;
        private ScorePhaseController _scorePhaseController;
        private PlayerTeamSetupController _teamSetupController;
        private GameModeDependencies _dependencies;

        // Dependencies
        protected LobbyService lobbyService => _dependencies?.LobbyService;
        protected SceneService sceneService => _dependencies?.SceneService;
        protected AudioService audioService => _dependencies?.AudioService;

        protected AugmentSelectionSystem augmentSelectionSys => _dependencies?.AugmentSelectionSystem;
        protected LevelSystem levelSystem => _dependencies?.LevelSystem;
        protected BombshellSystem bombshellSys => _dependencies?.BombshellSystem;
        protected CameraSystem cameraSystem => _dependencies?.CameraSystem;
        protected AnalyticsSystem analyticsSystem => _dependencies?.AnalyticsSystem;

        protected CountdownTimer timer;

        private AsyncOperationHandle<SO_GameModeData> _dataHandle;
        private AsyncOperationHandle<SO_GameFlowSettings> _flowSettingsHandle;
        private CancellationTokenSource _gameplayCancellation;
        
        public SO_GameModeData Data => _dataHandle.Result;
        public SO_GameFlowSettings FlowSettings => _flowSettingsHandle.Result;

        public virtual int MinPlayerCount => Data.MinPlayerCount;
        public virtual int MaxPlayerCount => Data.MaxPlayerCount;
        
        public MatchConfig MatchConfig { get; private set; } = MatchConfig.Default;

        public int ScoreToWin => MatchConfig.ScoreToWin;

        public bool IsReady
        {
            get
            {
                var players = lobbyService?.GetPlayers();

                if (players == null)
                    return false;

                return players.Count >= MinPlayerCount && players.Count <= MaxPlayerCount;
            }
        }

        public int CurrentRoundCount => _currentRound.RoundIndex;

        /// EVENTS
        public event Action<GameState> OnGameStateChanged;

        public event Action<PlayerManager, PlayerManager> OnPlayerKilled;

        public event Action OnGameStarted;
        public event Action<RoundInfo> OnRoundStarted;
        public event Action OnScoreDisplayOver;
        public event Action<RoundInfo> OnRoundEnded;
        public event Func<RoundInfo, UniTask> OnRoundEndedAsync;
        public event Action OnRaceStart;
        public event Func<CancellationToken, UniTask> OnAugmentRaceStartPresentationAsync;
        public event Func<UniTask> OnRaceEndedUI;
        public event Action<int> OnGameEnded;

        public virtual async UniTask Initialize()
        {
            _dependencies = GameModeDependencies.ResolveServices();

            if (!_dependencies.HasRequiredServices())
            {
                Logs.LogError("[GameModeBase] Missing required services.");
            }

            _dataHandle = await AddressablesUtils.LazyLoadAsset<SO_GameModeData>("DA_GM_FFA");
            _flowSettingsHandle = await AddressablesUtils.LazyLoadAsset<SO_GameFlowSettings>("DA_GameFlowSettings");

            timer = new CountdownTimer(0f);

            _roundStartController = new RoundStartController(
                timer,
                Data,
                SetPlayerControlContext,
                UpdateGameState,
                roundInfo => OnRoundStarted?.Invoke(roundInfo)
            );

            Logs.Log("Game mode initialized successfully.");
        }

        protected virtual List<PlayerTeam> CreateTeamsForMatch(IReadOnlyList<PlayerManager> players)
        {
            return _teamSetupController.CreateFreeForAllTeams(players);
        }

        protected virtual void ResolveGameplayDependencies()
        {
            _dependencies.ResolveGameplaySystems();

            if (!_dependencies.HasRequiredGameplaySystems())
            {
                Logs.LogError("[GameModeBase] Missing required gameplay systems.");
            }
        }

        protected virtual void CreateTeams()
        {
            _teamSetupController = new PlayerTeamSetupController();

            var players = lobbyService.GetPlayers();

            teams = CreateTeamsForMatch(players);
            Teams = teams.AsReadOnly();

            alivePlayers = new List<PlayerCharacter>();
            AlivePlayers = new ReadOnlyCollection<PlayerCharacter>(alivePlayers);
        }

        protected virtual void CreateControllers()
        {
            _playerSpawnController = new PlayerSpawnController(teams, levelSystem);

            _roundWinnerPresentationController = new RoundWinnerPresentationController(
                _playerSpawnController,
                cameraSystem
            );

            _scorePhaseController = new ScorePhaseController(
                teams,
                cameraSystem,
                UpdateGameState,
                () => _roundStartController.StopCountdown(),
                () => OnScoreDisplayOver?.Invoke()
            );

            _augmentRaceController = new AugmentRaceController(
                teams,
                augmentSelectionSys,
                _playerSpawnController,
                SetPlayerControlContext,
                () => OnRaceStart?.Invoke()
            );

            _roundController = new RoundController(teams, alivePlayers);
            _roundController.OnPlayerDied += HandleRoundPlayerDied;
            _roundController.OnPlayerKilled += HandleRoundPlayerKilled;

            AlivePlayers = _roundController.AlivePlayers;

            _scoreController = new ScoreController(
                Data,
                teams,
                ScoreToWin,
                analyticsSystem
            );
        }

        public virtual async UniTask StartGame()
        {
            ResolveGameplayDependencies();

            CreateTeams();

            CreateControllers();

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

            _gameplayCancellation = new CancellationTokenSource();
            GameplayCoroutine(_gameplayCancellation.Token).Forget();

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

        protected virtual async UniTask RunAugmentRacePhaseAsync(
            TransitionColor transitionColor,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            EnablePlayerGravity(false);

            await levelSystem.LoadRaceMap(true, transitionColor);
            cancellationToken.ThrowIfCancellationRequested();

            audioService.SetPhase(1);

            UpdateGameState(GameState.AugmentIntro);

            StartRace();

            await cameraSystem.Controller.ApplyCameraMapConfigAsync(maxWaitSeconds: 4f);
            cancellationToken.ThrowIfCancellationRequested();

            await _augmentRaceController.PrepareSelectionAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            audioService.SetPhase(0);

            await RunAugmentRaceStartPresentationAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            UpdateGameState(GameState.AugmentRace);
            SetPlayerControlContext(PlayerControlContext.AugmentRace);

            _augmentRaceController.StartRaceTimer(FlowSettings.AugmentRaceDuration);

            await _augmentRaceController.WaitUntilSelectionOverAsync(cancellationToken);

            _augmentRaceController.EndSelection();

            EndRace();

            EnablePlayerGravity(false);

            if (OnRaceEndedUI != null)
            {
                foreach (var @delegate in OnRaceEndedUI.GetInvocationList())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var handler = (Func<UniTask>)@delegate;
                    await handler.Invoke();

                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        protected virtual async UniTask WaitUntilRoundOverAsync(CancellationToken cancellationToken)
        {
            while (_roundController != null && !_roundController.OneTeamStanding)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await UniTask.Yield();

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        protected virtual async UniTask RunRoundEndPresentationAsync()
        {
            if (OnRoundEndedAsync == null)
                return;

            foreach (var @delegate in OnRoundEndedAsync.GetInvocationList())
            {
                var handler = (Func<RoundInfo, UniTask>)@delegate;
                await handler.Invoke(_currentRound);
            }
        }

        protected virtual UniTask RunScorePhaseAsync()
        {
            DisplayScores();

            HideScores();

            return UniTask.CompletedTask;
        }

        protected virtual async UniTask RunRoundPhaseAsync(
            TransitionColor transitionColor,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            await levelSystem.LoadArenaMap(true, transitionColor);
            cancellationToken.ThrowIfCancellationRequested();

            StartRound();

            await WaitUntilRoundOverAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            UpdateGameState(GameState.EndRound);

            EndRound();

            await RunRoundEndPresentationAsync();
            cancellationToken.ThrowIfCancellationRequested();

            await RunScorePhaseAsync();
            cancellationToken.ThrowIfCancellationRequested();
        }

        protected virtual async UniTask RunMatchLoopAsync(CancellationToken cancellationToken)
        {
            while (currentState != GameState.EndGame)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var transitionColor = GetTransitionColor();

                await RunAugmentRacePhaseAsync(transitionColor, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                if (currentState == GameState.EndGame)
                    break;

                await RunRoundPhaseAsync(transitionColor, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                if (IsGameOver(out gameVictor))
                {
                    Logs.Log($"Game Over! Team {gameVictor.Index} wins!");
                    UpdateGameState(GameState.EndGame);
                }
            }
        }

        protected async UniTaskVoid GameplayCoroutine(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                UpdateGameState(GameState.StartGame);
                OnGameStarted?.Invoke();

                audioService
                    .StartMusic(AudioService.FMODEvents.MUS_Gameplay)
                    .Forget();

                await RunMatchLoopAsync(cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                EndGame();
            }
            catch (OperationCanceledException)
            {
                Logs.Log("[GameModeBase] Gameplay coroutine canceled.");
            }
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

        private void EnablePlayerGravity(bool enabled = true)
        {
            _playerSpawnController?.SetPlayerGravity(enabled);
        }

        private void SpawnPlayers()
        {
            _playerSpawnController?.SpawnPlayers(_currentRound.RoundIndex);
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

        protected virtual void ResetPlayers()
        {
            _playerSpawnController?.ResetPlayers();
        }

        protected virtual void StartRound()
        {
            UpdateGameState(GameState.RoundCountdown);

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

            _roundStartController.StartCountdown(_currentRound);
        }

        protected virtual void EndRound()
        {
            _roundStartController.StopCountdown();

            _roundController.EndRound();

            bombshellSys.ClearActiveBombshells();

            EventBus<TriggerEndRound>.Raise(new TriggerEndRound());

            SetPlayerControlContext(PlayerControlContext.RoundEnded);

            EvaluateScores();

            _currentRound.WinningTeam = _roundController.WinningTeam;

            _roundWinnerPresentationController.PresentWinner(_currentRound.WinningTeam);

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
            _scorePhaseController?.DisplayScores();
        }

        protected virtual void HideScores()
        {
            _scorePhaseController?.HideScores();
        }

        protected virtual void StartRace()
        {
            _augmentRaceController.BeginRace(_currentRound.RoundIndex);
        }

        protected virtual void EndRace()
        {
            UpdateGameState(GameState.EndAugmentRace);

            _augmentRaceController.EndRace();
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
            audioService
                .StartMusic(AudioService.FMODEvents.MUS_Victory)
                .Forget();

            SetPlayerControlContext(PlayerControlContext.EndGame);
            OnGameEnded?.Invoke(GetWinnerPlayerIndex());
            Logs.Log("Game has ended.");
        }
        
        protected virtual async UniTask RunAugmentRaceStartPresentationAsync(CancellationToken cancellationToken)
        {
            if (OnAugmentRaceStartPresentationAsync == null)
                return;

            foreach (var @delegate in OnAugmentRaceStartPresentationAsync.GetInvocationList())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var handler = (Func<CancellationToken, UniTask>)@delegate;
                await handler.Invoke(cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        protected virtual void UpdateGameState(GameState newState)
        {
            currentState = newState;
            OnGameStateChanged?.Invoke(newState);
        }

        public void SetMatchConfig(MatchConfig config)
        {
            MatchConfig = config;

            _scoreController?.SetScoreToWin(config.ScoreToWin);
        }

        public void SetScoreToWin(int score)
        {
            SetMatchConfig(new MatchConfig(score));
        }

        public virtual void Update(){}

        public virtual void Dispose()
        {
            _gameplayCancellation?.Cancel();
            _gameplayCancellation?.Dispose();
            _gameplayCancellation = null;

            if (_dataHandle.IsValid())
            {
                Addressables.Release(_dataHandle);
            }
            if (_flowSettingsHandle.IsValid())
            {
                Addressables.Release(_flowSettingsHandle);
            }
            if (_roundController != null)
            {
                _roundController.OnPlayerDied -= HandleRoundPlayerDied;
                _roundController.OnPlayerKilled -= HandleRoundPlayerKilled;
                _roundController.Dispose();
                _roundController = null;
            }

            _roundStartController?.Dispose();
            _roundStartController = null;

            _scoreController = null;
            _playerSpawnController = null;
            _augmentRaceController = null;
            _roundWinnerPresentationController = null;
            _scorePhaseController = null;
            _teamSetupController = null;

            teams?.Clear();
            alivePlayers?.Clear();
            RoundHistory?.Clear();

            timer?.Dispose();
            timer = null;

            _dependencies = null;
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