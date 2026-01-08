using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;

namespace MortierFu
{
    /// <summary>
    /// Gère la liste des joueurs actifs dans le lobby et relaie leurs événements.
    /// </summary>
    public class LobbyService : IGameService
    {
        private readonly List<PlayerManager> _players = new();
        private int _maxPlayers = 4;

        public event Action<PlayerManager> OnPlayerJoined;
        public event Action<PlayerManager> OnPlayerLeft;
        
        public int CurrentPlayerCount => _players.Count;
        
        public void Dispose()
        {
            _players.Clear();
            Logs.Log("[LobbyService] Disposed.");
        }

        public void RegisterPlayer(PlayerManager player)
        {
            if (player == null || _players.Contains(player))
                return;

            if (_players.Count >= _maxPlayers)
            {
                Logs.LogWarning($"[LobbyService] Max players reached ({_maxPlayers}).");
                return;
            }

            _players.Add(player);
            player.OnPlayerInitialized += HandlePlayerInitialized;
            player.OnPlayerDestroyed += HandlePlayerDestroyed;

            Logs.Log($"[LobbyService] Player {player.PlayerIndex} joined the lobby.");
            OnPlayerJoined?.Invoke(player);
        }

        public void UnregisterPlayer(PlayerManager player)
        {
            if (player == null || !_players.Contains(player))
                return;

            player.OnPlayerInitialized -= HandlePlayerInitialized;
            player.OnPlayerDestroyed -= HandlePlayerDestroyed;

            _players.Remove(player);
            Logs.Log($"[LobbyService] Player {player.PlayerIndex} left the lobby.");
            OnPlayerLeft?.Invoke(player);
        }

        private void HandlePlayerInitialized(PlayerManager player)
        {
            // Peut être utile pour relancer une UI / ready check
            Logs.Log($"[LobbyService] Player {player.PlayerIndex} ready.");
        }

        private void HandlePlayerDestroyed(PlayerManager player)
        {
            UnregisterPlayer(player);
        }

        public IReadOnlyList<PlayerManager> GetPlayers() => _players.AsReadOnly();

        public UniTask OnInitialize()
        {
            return UniTask.CompletedTask;
        }

        public bool IsInitialized { get; set; }
    }
}
