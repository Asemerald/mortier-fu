using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbyStartReadyController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LobbyStartTarget _startTarget;
        [SerializeField] private LobbySandboxController _sandboxController;
        [SerializeField] private LobbySandboxStateController _stateController;
        [SerializeField] private LobbyMatchLauncher _matchLauncher;

        [Header("Feedback")]
        [SerializeField] private GameObject[] _playerReadyIndicators;

        [Header("Rules")]
        [SerializeField] private float _toggleCooldown = 0.3f;

        private readonly HashSet<PlayerManager> _readyPlayers = new();
        private readonly HashSet<PlayerManager> _registeredPlayers = new();
        private readonly Dictionary<PlayerManager, float> _lastToggleTimes = new();

        private void Awake()
        {
            if (!_startTarget)
                _startTarget = GetComponent<LobbyStartTarget>();

            if (!_sandboxController)
                _sandboxController = GetComponent<LobbySandboxController>();

            if (!_stateController)
                _stateController = GetComponent<LobbySandboxStateController>();

            if (!_matchLauncher)
                _matchLauncher = GetComponent<LobbyMatchLauncher>();
        }

        private void OnEnable()
        {
            if (_startTarget)
                _startTarget.OnHitByPlayer += HandleStartTargetHit;

            if (_sandboxController)
            {
                _sandboxController.OnPlayerSpawned += RegisterPlayer;

                var players = _sandboxController.GetSpawnedPlayers();

                for (int i = 0; i < players.Count; i++)
                {
                    RegisterPlayer(players[i]);
                }
            }

            RefreshFeedback();
        }

        private void OnDisable()
        {
            if (_startTarget)
                _startTarget.OnHitByPlayer -= HandleStartTargetHit;

            if (_sandboxController)
                _sandboxController.OnPlayerSpawned -= RegisterPlayer;

            UnregisterAllPlayers();
            _readyPlayers.Clear();
            _lastToggleTimes.Clear();

            RefreshFeedback();
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
            if (player is null)
                return;

            if (!_registeredPlayers.Remove(player))
                return;

            player.OnPlayerDestroyed -= HandlePlayerDestroyed;
        }

        private void UnregisterAllPlayers()
        {
            var players = new List<PlayerManager>(_registeredPlayers);

            for (var i = 0; i < players.Count; i++)
            {
                UnregisterPlayer(players[i]);
            }

            _registeredPlayers.Clear();
        }

        private void HandlePlayerDestroyed(PlayerManager player)
        {
            if (player is null)
                return;

            _readyPlayers.Remove(player);
            _lastToggleTimes.Remove(player);

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

            ToggleReady(player);
        }

        private bool CanToggleReady(PlayerManager player)
        {
            if (!player)
                return false;

            float now = Time.unscaledTime;

            if (_lastToggleTimes.TryGetValue(player, out float lastToggleTime))
            {
                if (now - lastToggleTime < _toggleCooldown)
                    return false;
            }

            _lastToggleTimes[player] = now;
            return true;
        }

        private void ToggleReady(PlayerManager player)
        {
            if (!player)
                return;

            if (!_readyPlayers.Add(player))
            {
                _readyPlayers.Remove(player);
                Logs.Log($"[LobbyStartReadyController] Player {player.PlayerIndex + 1} is no longer ready.");
            }
            else
            {
                Logs.Log($"[LobbyStartReadyController] Player {player.PlayerIndex + 1} is ready.");
            }

            RefreshFeedback();
            TryLaunchIfAllReady();
        }

        private bool IsPlayerInSandbox(PlayerManager player)
        {
            if (!player)
                return false;

            if (!_sandboxController)
                return false;

            var players = _sandboxController.GetSpawnedPlayers();

            for (var i = 0; i < players.Count; i++)
            {
                if (ReferenceEquals(players[i], player))
                    return true;
            }

            return false;
        }

        private void TryLaunchIfAllReady()
        {
            if (!_sandboxController)
                return;

            var players = _sandboxController.GetSpawnedPlayers();

            if (_matchLauncher && !_matchLauncher.CanLaunch(players))
                return;

            var validPlayerCount = 0;

            for (var i = 0; i < players.Count; i++)
            {
                var player = players[i];

                if (!player)
                    continue;

                validPlayerCount++;

                if (!_readyPlayers.Contains(player))
                    return;
            }

            if (validPlayerCount <= 0)
                return;

            if (!_matchLauncher)
            {
                Logs.LogError("[LobbyStartReadyController] MatchLauncher reference is missing.");
                return;
            }

            Logs.Log("[LobbyStartReadyController] All sandbox players are ready.");

            _matchLauncher.LaunchMatch();
        }

        private void RefreshFeedback()
        {
            if (_playerReadyIndicators is null)
                return;

            for (var i = 0; i < _playerReadyIndicators.Length; i++)
            {
                var indicator = _playerReadyIndicators[i];

                if (!indicator)
                    continue;

                indicator.SetActive(IsPlayerIndexReady(i));
            }
        }

        private bool IsPlayerIndexReady(int playerIndex)
        {
            foreach (var player in _readyPlayers)
            {
                if (player && player.PlayerIndex == playerIndex)
                    return true;
            }

            return false;
        }

        public void ResetReady()
        {
            _readyPlayers.Clear();
            _lastToggleTimes.Clear();

            RefreshFeedback();

            Logs.Log("[LobbyStartReadyController] Ready state reset.");
        }
    }
}