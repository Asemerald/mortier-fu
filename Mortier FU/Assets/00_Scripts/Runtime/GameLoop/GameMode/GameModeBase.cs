using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Cysharp.Threading.Tasks;
using MortierFu.Analytics;
using MortierFu.Shared;
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
        private PlayerSpawnController _playerSpawnController;
        private AugmentRaceController _augmentRaceController;
        
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

        public MatchConfig MatchConfig { get; private set; } = MatchConfig.Default;

        public int ScoreToWin => MatchConfig.ScoreToWin;

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
            _playerSpawnController = new PlayerSpawnController(teams, levelSystem);

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
        
        protected virtual async UniTask RunAugmentRacePhaseAsync(TransitionColor transitionColor)
        {
            EnablePlayerGravity(false);

            await levelSystem.LoadRaceMap(true, transitionColor);

            ServiceManager.Instance.Get<AudioService>().SetPhase(1);

            UpdateGameState(GameState.DisplayAugment);

            StartRace();

            await cameraSystem.Controller.ApplyCameraMapConfigAsync(maxWaitSeconds: 4f);

            await _augmentRaceController.HandleSelectionAsync(Data.AugmentSelectionDuration);

            ServiceManager.Instance.Get<AudioService>().SetPhase(0);

            UpdateGameState(GameState.RaceInProgress);

            await _augmentRaceController.WaitUntilSelectionOverAsync();

            _augmentRaceController.EndSelection();

            EndRace();

            EnablePlayerGravity(false);

            if (OnRaceEndedUI != null)
            {
                foreach (var @delegate in OnRaceEndedUI.GetInvocationList())
                {
                    var handler = (Func<UniTask>)@delegate;
                    await handler.Invoke();
                }
            }
        }
        
        protected virtual async UniTask RunRoundPhaseAsync(TransitionColor transitionColor)
        {
            await levelSystem.LoadArenaMap(true, transitionColor);

            StartRound();

            while (_roundController != null && !_roundController.OneTeamStanding)
            {
                await UniTask.Yield();
            }

            UpdateGameState(GameState.EndRound);

            EndRound();

            if (OnRoundEndedAsync != null)
            {
                foreach (var @delegate in OnRoundEndedAsync.GetInvocationList())
                {
                    var handler = (Func<RoundInfo, UniTask>)@delegate;
                    await handler.Invoke(_currentRound);
                }
            }

            UpdateGameState(GameState.DisplayScores);

            HideScores();
        }

        protected async UniTaskVoid GameplayCoroutine()
        {
            UpdateGameState(GameState.StartGame);
            OnGameStarted?.Invoke();

            ServiceManager.Instance.Get<AudioService>()
                .StartMusic(AudioService.FMODEvents.MUS_Gameplay)
                .Forget();

            while (currentState != GameState.EndGame)
            {
                var transitionColor = GetTransitionColor();

                await RunAugmentRacePhaseAsync(transitionColor);

                await RunRoundPhaseAsync(transitionColor);

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

        private void EnablePlayerGravity(bool enabled = true)
        {
            _playerSpawnController?.SetPlayerGravity(enabled);
        }

        private void SpawnPlayers()
        {
            _playerSpawnController?.SpawnPlayers(_currentRound.RoundIndex);
        }

        private void SpawnWinnerTeam(PlayerTeam winnerTeam)
        {
            _playerSpawnController?.SpawnWinnerTeam(winnerTeam);
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
            _playerSpawnController?.ResetPlayers();
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

            _augmentRaceController.BeginRace(_currentRound.RoundIndex);
        }

        protected virtual void EndRace()
        {
            UpdateGameState(GameState.EndingRace);

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

        public void SetMatchConfig(MatchConfig config)
        {
            MatchConfig = config;

            _scoreController?.SetScoreToWin(config.ScoreToWin);
        }

        public void SetScoreToWin(int score)
        {
            SetMatchConfig(new MatchConfig(score));
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
            _playerSpawnController = null;
            _augmentRaceController = null;

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