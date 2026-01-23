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
        public readonly List<PlayerManager> Players = new();
        private int _maxPlayers = 4;

        public event Action<PlayerManager> OnPlayerJoined;
        public event Action<PlayerManager> OnPlayerLeft;
        
        public int CurrentPlayerCount => Players.Count;
        
        public void Dispose()
        {
            Players.Clear();
            Logs.Log("[LobbyService] Disposed.");
        }

        public void RegisterPlayer(PlayerManager player)
        {
            if (player == null || Players.Contains(player))
                return;
            
            if (Players.Count >= _maxPlayers)
            {
                Logs.LogWarning($"[LobbyService] Max players reached ({_maxPlayers}).");
                return;
            }

            Players.Add(player);
            ServiceManager.Instance.Get<ShakeService>().ShakeController(player, ShakeService.ShakeType.MID);
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Join);
            player.OnPlayerInitialized += HandlePlayerInitialized;
            player.OnPlayerDestroyed += HandlePlayerDestroyed;

            Logs.Log($"[LobbyService] Player {player.PlayerIndex} joined the lobby.");
            OnPlayerJoined?.Invoke(player);
        }

        public void UnregisterPlayer(PlayerManager player)
        {
            if (player == null || !Players.Contains(player))
                return;

            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Return);
            player.OnPlayerInitialized -= HandlePlayerInitialized;
            player.OnPlayerDestroyed -= HandlePlayerDestroyed;

            Players.Remove(player);
            Logs.Log($"[LobbyService] Player {player.PlayerIndex} left the lobby.");
            OnPlayerLeft?.Invoke(player);
        }

        private void HandlePlayerInitialized(PlayerManager player)
        {
            // Peut être utile pour relancer une UI / ready check
            //Logs.Log($"[LobbyService] Player {player.PlayerIndex} ready.");
        }

        private void HandlePlayerDestroyed(PlayerManager player)
        {
            UnregisterPlayer(player);
        }

        public IReadOnlyList<PlayerManager> GetPlayers() => Players.AsReadOnly();

        public UniTask OnInitialize()
        {
            return UniTask.CompletedTask;
        }
        
        public PlayerManager GetPlayerByIndex(int index)
        {
            return Players[index];
        }

        public bool IsInitialized { get; set; }
    }
}
