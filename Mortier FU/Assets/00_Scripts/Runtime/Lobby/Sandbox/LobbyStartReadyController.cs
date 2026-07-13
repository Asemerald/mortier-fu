using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.Serialization;

namespace MortierFu
{
    public sealed class LobbyStartReadyController : MonoBehaviour
    {
        private static readonly int IsReadyHash = Animator.StringToHash("IsReady");
        private static readonly int IsCanceledHash = Animator.StringToHash("IsCanceled");

        [Header("References")]
        [SerializeField] private LobbyStartTarget _startTarget;
        [SerializeField] private LobbySandboxController _sandboxController;
        [SerializeField] private LobbySandboxStateController _stateController;
        [SerializeField] private LobbyMatchLauncher _matchLauncher;

        [Header("Feedback")]
        [SerializeField] private GameObject[] _playerReadyIndicators;

        [FormerlySerializedAs("delayIndicators")]
        [SerializeField, Min(0f)] private float _indicatorDelay = 0.1f;

        [Header("Launch")]
        [SerializeField, Min(0f)] private float _launchDelayAfterAllReady = 1.3f;

        [Header("Rules")]
        [SerializeField, Min(0f)] private float _toggleCooldown = 0.3f;

        private readonly HashSet<PlayerManager> _readyPlayers = new();
        private readonly HashSet<PlayerManager> _registeredPlayers = new();
        private readonly Dictionary<PlayerManager, float> _lastToggleTimes = new();

        private Animator[] _indicatorAnimators;

        private CancellationTokenSource _feedbackCancellation;
        private CancellationTokenSource _launchCancellation;

        private PlayerManager _lastShooter;

        private void Awake() => CacheIndicatorAnimators();

        private void OnEnable()
        {
            if (_startTarget)
                _startTarget.OnHitByPlayer += HandleStartTargetHit;

            if (_sandboxController)
            {
                _sandboxController.OnPlayerSpawned += RegisterPlayer;

                var players = _sandboxController.GetSpawnedPlayers();

                for (int i = 0; i < players.Count; i++)
                    RegisterPlayer(players[i]);
            }

            RefreshFeedback();
        }

        private void OnDisable()
        {
            CancelFeedbackTask();
            CancelLaunchTask();

            if (_startTarget)
                _startTarget.OnHitByPlayer -= HandleStartTargetHit;

            if (_sandboxController)
                _sandboxController.OnPlayerSpawned -= RegisterPlayer;

            UnregisterAllPlayers();

            _readyPlayers.Clear();
            _lastToggleTimes.Clear();
            _lastShooter = null;

            UpdateAllIndicatorsInstant();
        }

        private void OnDestroy()
        {
            CancelFeedbackTask();
            CancelLaunchTask();
        }

        private void RegisterPlayer(PlayerManager player)
        {
            if (!player)
                return;

            if (!_registeredPlayers.Add(player))
                return;

            player.OnPlayerDestroyed += HandlePlayerDestroyed;

            RefreshFeedback();
        }

        private void UnregisterPlayer(PlayerManager player)
        {
            if (!player)
                return;

            if (!_registeredPlayers.Remove(player))
                return;

            player.OnPlayerDestroyed -= HandlePlayerDestroyed;
        }

        private void UnregisterAllPlayers()
        {
            List<PlayerManager> players = new(_registeredPlayers);

            for (int i = 0; i < players.Count; i++)
                UnregisterPlayer(players[i]);

            _registeredPlayers.Clear();
        }

        private void HandlePlayerDestroyed(PlayerManager player)
        {
            if (!player)
                return;

            _readyPlayers.Remove(player);
            _lastToggleTimes.Remove(player);

            if (ReferenceEquals(_lastShooter, player))
                _lastShooter = null;

            CancelLaunchTask();

            UnregisterPlayer(player);
            RefreshFeedback();
        }

        private void HandleStartTargetHit(PlayerManager player)
        {
            if (!player)
                return;

            if (_stateController && !_stateController.CanUseStartTarget())
                return;

            if (!IsPlayerInSandbox(player))
                return;

            if (!CanToggleReady(player))
                return;

            _lastShooter = player;

            ToggleReady(player);
        }

        private bool CanToggleReady(PlayerManager player)
        {
            float now = Time.unscaledTime;

            if (_lastToggleTimes.TryGetValue(player, out float lastTime))
            {
                if (now - lastTime < _toggleCooldown)
                    return false;
            }

            _lastToggleTimes[player] = now;
            return true;
        }

        private void ToggleReady(PlayerManager player)
        {
            bool isReady = _readyPlayers.Add(player);

            if (isReady)
            {
                _startTarget?.PlayDongAnimation();
                Logs.Log($"[LobbyStartReadyController] Player {player.PlayerIndex + 1} is ready.");
            }
            else
            {
                _readyPlayers.Remove(player);
                CancelLaunchTask();
                Logs.Log($"[LobbyStartReadyController] Player {player.PlayerIndex + 1} is no longer ready.");
            }

            UpdateFeedbackAnimation(player.PlayerIndex);

            if (isReady)
                ScheduleLaunchIfAllReady(player);
        }

        private bool IsPlayerInSandbox(PlayerManager player)
        {
            if (!player || !_sandboxController)
                return false;

            IReadOnlyList<PlayerManager> players = _sandboxController.GetSpawnedPlayers();

            for (int i = 0; i < players.Count; i++)
            {
                if (ReferenceEquals(players[i], player))
                    return true;
            }

            return false;
        }

        private void ScheduleLaunchIfAllReady(PlayerManager requester)
        {
            if (_launchCancellation != null)
                return;

            if (!AreAllPlayersReady())
                return;

            _launchCancellation = new CancellationTokenSource();

            LaunchAfterDelayAsync(requester, _launchCancellation.Token).Forget();
        }

        private async UniTaskVoid LaunchAfterDelayAsync(PlayerManager requester, CancellationToken cancellationToken)
        {
            try
            {
                if (_launchDelayAfterAllReady > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(_launchDelayAfterAllReady), DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, cancellationToken);

                TryLaunchIfAllReady(requester);
            }
            catch (OperationCanceledException)
            { }
            finally
            {
                CancelLaunchTask();
            }
        }

        private bool AreAllPlayersReady()
        {
            if (!_sandboxController)
                return false;

            IReadOnlyList<PlayerManager> players = _sandboxController.GetSpawnedPlayers();

            if (_matchLauncher && !_matchLauncher.CanLaunch(players))
                return false;

            int validPlayerCount = 0;

            for (int i = 0; i < players.Count; i++)
            {
                PlayerManager player = players[i];

                if (!player)
                    continue;

                validPlayerCount++;

                if (!_readyPlayers.Contains(player))
                    return false;
            }

            return validPlayerCount > 0;
        }

        private void TryLaunchIfAllReady(PlayerManager requester)
        {
            if (!AreAllPlayersReady())
                return;

            if (!_matchLauncher)
            {
                Logs.LogError("[LobbyStartReadyController] MatchLauncher reference is missing.");
                return;
            }

            PlayerManager launchPlayer = requester ? requester : GetFirstSandboxPlayer();

            if (!launchPlayer)
            {
                Logs.LogError("[LobbyStartReadyController] Cannot launch match. No valid player found.");
                return;
            }

            Logs.Log("[LobbyStartReadyController] All sandbox players are ready.");
            _matchLauncher.LaunchMatch(launchPlayer);
        }

        private PlayerManager GetFirstSandboxPlayer()
        {
            if (!_sandboxController)
                return null;

            var players = _sandboxController.GetSpawnedPlayers();

            for (int i = 0; i < players.Count; i++)
            {
                if (players[i])
                    return players[i];
            }

            return null;
        }

        public void StartMatch() => ScheduleLaunchIfAllReady(_lastShooter);

        private void RefreshFeedback()
        {
            CancelFeedbackTask();

            int startIndex = GetHighestRegisteredPlayerIndex();

            if (startIndex < 0)
                return;

            _feedbackCancellation = new CancellationTokenSource();
            UpdateAllVisualAsync(startIndex, _feedbackCancellation.Token).Forget();
        }

        private async UniTaskVoid UpdateAllVisualAsync(int startIndex, CancellationToken cancellationToken)
        {
            try
            {
                for (int i = startIndex; i >= 0; i--)
                {
                    UpdateFeedbackAnimation(i);

                    if (i <= 0 || _indicatorDelay <= 0f)
                        continue;

                    await UniTask.Delay(TimeSpan.FromSeconds(_indicatorDelay), DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            { }
        }

        private void UpdateAllIndicatorsInstant()
        {
            if (_indicatorAnimators == null)
                return;

            for (int i = 0; i < _indicatorAnimators.Length; i++)
                UpdateFeedbackAnimation(i);
        }

        private void UpdateFeedbackAnimation(int playerIndex)
        {
            if (_indicatorAnimators == null)
                return;

            if (playerIndex < 0 || playerIndex >= _indicatorAnimators.Length)
                return;

            Animator animator = _indicatorAnimators[playerIndex];

            if (!animator)
                return;

            bool isReady = IsPlayerIndexReady(playerIndex);

            animator.SetBool(IsCanceledHash, !isReady);
            animator.SetBool(IsReadyHash, isReady);
        }

        private bool IsPlayerIndexReady(int playerIndex)
        {
            foreach (PlayerManager player in _readyPlayers)
            {
                if (player && player.PlayerIndex == playerIndex)
                    return true;
            }

            return false;
        }

        private int GetHighestRegisteredPlayerIndex()
        {
            int highest = -1;

            foreach (PlayerManager player in _registeredPlayers)
            {
                if (!player)
                    continue;

                highest = Mathf.Max(highest, player.PlayerIndex);
            }

            if (_indicatorAnimators == null || _indicatorAnimators.Length == 0)
                return -1;

            return Mathf.Min(highest, _indicatorAnimators.Length - 1);
        }

        private void CacheIndicatorAnimators()
        {
            if (_playerReadyIndicators == null)
            {
                _indicatorAnimators = Array.Empty<Animator>();
                return;
            }

            _indicatorAnimators = new Animator[_playerReadyIndicators.Length];

            for (int i = 0; i < _playerReadyIndicators.Length; i++)
            {
                if (_playerReadyIndicators[i])
                    _indicatorAnimators[i] = _playerReadyIndicators[i].GetComponent<Animator>();
            }
        }

        private void CancelFeedbackTask()
        {
            _feedbackCancellation?.Cancel();
            _feedbackCancellation?.Dispose();
            _feedbackCancellation = null;
        }

        private void CancelLaunchTask()
        {
            _launchCancellation?.Cancel();
            _launchCancellation?.Dispose();
            _launchCancellation = null;
        }

        public void ResetReady()
        {
            CancelLaunchTask();

            _readyPlayers.Clear();
            _lastToggleTimes.Clear();
            _lastShooter = null;

            RefreshFeedback();

            Logs.Log("[LobbyStartReadyController] Ready state reset.");
        }
    }
}