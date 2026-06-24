using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class ConfirmationService : IGameService, IPlayerUIInputHandler
    {
        private readonly HashSet<PlayerManager> _pendingPlayers = new();

        private LobbyService _lobbyService;
        private PlayerUIInputService _uiInputService;
        private ShakeService _shakeService;

        private TaskCompletionSource<bool> _completionSource;

        private bool _isWaitingForConfirmation;
        private int _confirmationCount;

        public bool IsInitialized { get; set; }

        public event Action<int> OnPlayerConfirmed;
        public event Action OnAllPlayersConfirmed;
        public event Action<int> OnStartConfirmation;

        public UniTask OnInitialize()
        {
            _lobbyService = ServiceManager.Instance.Get<LobbyService>();
            _uiInputService = ServiceManager.Instance.Get<PlayerUIInputService>();
            _shakeService = ServiceManager.Instance.Get<ShakeService>();

            return UniTask.CompletedTask;
        }

        public async Task WaitUntilHostConfirmed()
        {
            var host = GetPlayerByIndex(0);

            if (!host)
            {
                Logs.LogError("[ConfirmationService] No PlayerManager found for host Player 1.");
                return;
            }

            BeginConfirmation(new[] { host });

            await WaitForCompletion();

            OnAllPlayersConfirmed?.Invoke();

            Logs.Log("[ConfirmationService] Host confirmed.");
        }

        public void ShowConfirmation(int activePlayers)
        {
            OnStartConfirmation?.Invoke(activePlayers);
            Logs.Log("[ConfirmationService] Confirmation started.");
        }

        public async Task WaitUntilAllConfirmed()
        {
            var players = GetAvailablePlayers();

            if (players.Count == 0)
            {
                Logs.LogWarning("[ConfirmationService] No players available for confirmation.");
                OnAllPlayersConfirmed?.Invoke();
                return;
            }

            BeginConfirmation(players);

            await WaitForCompletion();

            OnAllPlayersConfirmed?.Invoke();

            Logs.Log("[ConfirmationService] All players confirmed.");
        }

        private void BeginConfirmation(IEnumerable<PlayerManager> players)
        {
            if (_isWaitingForConfirmation)
            {
                Logs.LogWarning("[ConfirmationService] A confirmation was already active. It has been cleared.");
                ClearConfirmation(completeCurrentWait: true);
            }

            if (_uiInputService is null)
            {
                Logs.LogError("[ConfirmationService] PlayerUIInputService is missing. Cannot request confirmation.");
                return;
            }

            _completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingPlayers.Clear();

            foreach (var player in players)
            {
                if (!player)
                    continue;

                if (!_pendingPlayers.Add(player))
                    continue;

                _uiInputService.Push(player, this);
            }

            _confirmationCount = _pendingPlayers.Count;
            _isWaitingForConfirmation = _confirmationCount > 0;

            if (!_isWaitingForConfirmation)
            {
                CompleteConfirmation();
                return;
            }

            Logs.Log($"[ConfirmationService] Waiting for {_confirmationCount} player confirmation(s).");
        }

        private async Task WaitForCompletion()
        {
            if (_completionSource is null)
                return;

            await _completionSource.Task;
        }

        private void ConfirmPlayer(PlayerManager player)
        {
            if (!_isWaitingForConfirmation)
                return;

            if (!player)
                return;

            if (!_pendingPlayers.Remove(player))
                return;

            _uiInputService?.Remove(player, this);

            _confirmationCount = _pendingPlayers.Count;

            OnPlayerConfirmed?.Invoke(player.PlayerIndex);

            if (_shakeService is not null)
            {
                _shakeService.ShakeController(player, ShakeService.ShakeType.MID);
            }

            Logs.Log($"[ConfirmationService] Player {player.PlayerIndex + 1} confirmed. Remaining: {_confirmationCount}.");

            if (_confirmationCount <= 0)
            {
                CompleteConfirmation();
            }
        }

        private void CompleteConfirmation()
        {
            if (!_isWaitingForConfirmation && _completionSource is null)
                return;

            _isWaitingForConfirmation = false;
            _confirmationCount = 0;

            ClearPendingPlayerHandlers();

            _completionSource?.TrySetResult(true);
            _completionSource = null;
        }

        private void ClearConfirmation(bool completeCurrentWait)
        {
            _isWaitingForConfirmation = false;
            _confirmationCount = 0;

            ClearPendingPlayerHandlers();

            if (completeCurrentWait)
            {
                _completionSource?.TrySetResult(true);
            }

            _completionSource = null;
        }

        private void ClearPendingPlayerHandlers()
        {
            if (_pendingPlayers.Count == 0)
                return;

            var players = new List<PlayerManager>(_pendingPlayers);

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];

                if (!player)
                    continue;

                _uiInputService?.Remove(player, this);
            }

            _pendingPlayers.Clear();
        }

        private List<PlayerManager> GetAvailablePlayers()
        {
            var result = new List<PlayerManager>();

            if (_lobbyService is null)
                return result;

            var players = _lobbyService.GetPlayers();

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];

                if (!player)
                    continue;

                if (!result.Contains(player))
                    result.Add(player);
            }

            return result;
        }

        private PlayerManager GetPlayerByIndex(int playerIndex)
        {
            if (_lobbyService is null)
                return null;

            var players = _lobbyService.GetPlayers();

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];

                if (!player)
                    continue;

                if (player.PlayerIndex == playerIndex)
                    return player;
            }

            return null;
        }

        public bool CanHandleUIInput(PlayerManager player)
        {
            return _isWaitingForConfirmation &&
                   player &&
                   _pendingPlayers.Contains(player);
        }

        public bool HandleNavigate(PlayerManager player, Vector2 direction)
        {
            return false;
        }

        public bool HandleSubmit(PlayerManager player)
        {
            if (!CanHandleUIInput(player))
                return false;

            ConfirmPlayer(player);
            return true;
        }

        public bool HandleCancel(PlayerManager player)
        {
            return false;
        }

        public void Dispose()
        {
            ClearConfirmation(completeCurrentWait: true);

            OnPlayerConfirmed = null;
            OnAllPlayersConfirmed = null;
            OnStartConfirmation = null;
        }
    }
}