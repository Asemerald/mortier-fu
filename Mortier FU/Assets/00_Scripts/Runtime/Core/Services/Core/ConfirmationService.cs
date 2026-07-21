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
        private readonly HashSet<PlayerManager> _participants = new();
        private readonly HashSet<PlayerManager> _pendingPlayers = new();
        private readonly List<PlayerManager> _playersBuffer = new(4);

        private LobbyService _lobbyService;
        private PlayerUIInputService _uiInputService;
        private ShakeService _shakeService;

        private TaskCompletionSource<bool> _completionSource;
        private bool _isWaitingForConfirmation;

        public bool IsInitialized { get; set; }

        public event Action<int> OnPlayerConfirmed;
        public event Action<int> OnPlayerConfirmedAgain;
        public event Action OnAllPlayersConfirmed;
        public event Action<int> OnStartConfirmation;
        
        public int PendingPlayersCount => _pendingPlayers.Count;
        public int PlayersParticipantsCount => _participants.Count;

        public UniTask OnInitialize()
        {
            _lobbyService = ServiceManager.Instance.Get<LobbyService>();
            _uiInputService = ServiceManager.Instance.Get<PlayerUIInputService>();
            _shakeService = ServiceManager.Instance.Get<ShakeService>();

            return UniTask.CompletedTask;
        }

        public void ShowConfirmation(int activePlayers)
        {
            OnStartConfirmation?.Invoke(activePlayers);
            Logs.Log("[ConfirmationService] Confirmation started.");
        }

        public async Task<bool> WaitUntilAllConfirmed()
        {
            List<PlayerManager> players = GetAvailablePlayers();

            if (players.Count == 0)
            {
                Logs.LogWarning("[ConfirmationService] No players available for confirmation.");
                return false;
            }

            BeginConfirmation(players);

            bool confirmed = await WaitForCompletion();

            if (!confirmed)
                return false;

            OnAllPlayersConfirmed?.Invoke();

            Logs.Log("[ConfirmationService] All players confirmed.");

            return true;
        }

        public void ResetRuntimeState() => FinishConfirmation(false);

        public void BeginConfirmation(IEnumerable<PlayerManager> players)
        {
            if (_isWaitingForConfirmation)
            {
                Logs.LogWarning("[ConfirmationService] A confirmation was already active. It has been canceled.");
                FinishConfirmation(false);
            }

            if (_uiInputService is null)
            {
                Logs.LogError("[ConfirmationService] PlayerUIInputService is missing. Cannot request confirmation.");
                return;
            }

            _completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            _participants.Clear();
            _pendingPlayers.Clear();

            foreach (PlayerManager player in players)
            {
                if (!player)
                    continue;

                if (!_participants.Add(player))
                    continue;

                _pendingPlayers.Add(player);
                _uiInputService.Push(player, this);
            }

            _isWaitingForConfirmation = _pendingPlayers.Count > 0;

            if (!_isWaitingForConfirmation)
            {
                FinishConfirmation(true);
                return;
            }

            Logs.Log($"[ConfirmationService] Waiting for {_pendingPlayers.Count} player confirmation(s).");
        }

        private async Task<bool> WaitForCompletion()
        {
            if (_completionSource is null)
                return false;

            return await _completionSource.Task;
        }

        private void ConfirmPlayer(PlayerManager player)
        {
            if (!_isWaitingForConfirmation || !player)
                return;

            if (!_pendingPlayers.Remove(player))
                return;

            OnPlayerConfirmed?.Invoke(player.PlayerIndex);
            _shakeService?.ShakeController(player, ShakeService.ShakeType.MID);

            Logs.Log($"[ConfirmationService] Player {player.PlayerIndex + 1} confirmed. Remaining: {_pendingPlayers.Count}.");

            if (_pendingPlayers.Count <= 0)
                FinishConfirmation(true);
        }

        private void FinishConfirmation(bool result)
        {
            if (!_isWaitingForConfirmation && _completionSource is null)
                return;

            _isWaitingForConfirmation = false;

            ClearPlayerHandlers();

            _completionSource?.TrySetResult(result);
            _completionSource = null;
        }

        private void ClearPlayerHandlers()
        {
            if (_participants.Count > 0)
            {
                _playersBuffer.Clear();

                foreach (PlayerManager player in _participants)
                {
                    if (player)
                        _playersBuffer.Add(player);
                }

                for (int i = 0; i < _playersBuffer.Count; i++)
                {
                    _uiInputService?.Remove(_playersBuffer[i], this);
                }

                _playersBuffer.Clear();
            }

            _pendingPlayers.Clear();
            _participants.Clear();
        }

        public List<PlayerManager> GetAvailablePlayers()
        {
            var result = new List<PlayerManager>();

            if (_lobbyService is null)
                return result;

            IReadOnlyList<PlayerManager> players = _lobbyService.GetPlayers();

            for (int i = 0; i < players.Count; i++)
            {
                PlayerManager player = players[i];

                if (!player)
                    continue;

                if (!result.Contains(player))
                    result.Add(player);
            }

            return result;
        }

        public bool CanHandleUIInput(PlayerManager player) => _isWaitingForConfirmation && player && _participants.Contains(player);

        public bool HandleNavigate(PlayerManager player, Vector2 direction) => false;
        
        public bool HandleSubmit(PlayerManager player)
        {
            if (!CanHandleUIInput(player))
                return false;

            if (_pendingPlayers.Contains(player))
            {
                ConfirmPlayer(player);
                return true;
            }

            OnPlayerConfirmedAgain?.Invoke(player.PlayerIndex);
            return true;
        }

        public bool HandleCancel(PlayerManager player) => false;

        public void Dispose()
        {
            FinishConfirmation(false);

            OnPlayerConfirmed = null;
            OnPlayerConfirmedAgain = null;
            OnAllPlayersConfirmed = null;
            OnStartConfirmation = null;
        }
    }
}