using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MortierFu
{
    public class LobbyService : IGameService
    {
        public readonly List<PlayerManager> Players = new();

        private int _maxPlayers = 4;

        public event Action<PlayerManager> OnPlayerJoined;
        public event Action<PlayerManager> OnPlayerLeft;

        public int CurrentPlayerCount => Players.Count;

        public bool IsInitialized { get; set; }

        public UniTask OnInitialize()
        {
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            ClearPlayers(destroyPlayerObjects: false);

            OnPlayerJoined = null;
            OnPlayerLeft = null;

            Logs.Log("[LobbyService] Disposed.");
        }

        public IReadOnlyList<PlayerManager> GetPlayers()
        {
            return Players.AsReadOnly();
        }

        public PlayerManager GetPlayerByIndex(int playerIndex)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                var player = Players[i];

                if (!player)
                    continue;

                if (player.PlayerIndex == playerIndex)
                    return player;
            }

            return null;
        }

        public bool TryGetPlayerByIndex(int playerIndex, out PlayerManager player)
        {
            player = GetPlayerByIndex(playerIndex);
            return player;
        }

        public void RegisterPlayer(PlayerManager player)
        {
            if (!player)
                return;

            if (Players.Contains(player))
                return;

            if (Players.Count >= _maxPlayers)
            {
                Logs.LogWarning($"[LobbyService] Max players reached ({_maxPlayers}).");
                return;
            }

            Players.Add(player);

#if !UNITY_EDITOR
            var shakeService = ServiceManager.Instance?.Get<ShakeService>();
            shakeService?.ShakeController(player, ShakeService.ShakeType.MID);

            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Join);
#endif

            player.OnPlayerInitialized -= HandlePlayerInitialized;
            player.OnPlayerDestroyed -= HandlePlayerDestroyed;

            player.OnPlayerInitialized += HandlePlayerInitialized;
            player.OnPlayerDestroyed += HandlePlayerDestroyed;

            Logs.Log($"[LobbyService] Player {player.PlayerIndex + 1} registered.");

            OnPlayerJoined?.Invoke(player);
        }

        public void UnregisterPlayer(PlayerManager player)
        {
            if (!player)
                return;

            if (!Players.Contains(player))
                return;

            UnbindPlayerEvents(player);

            Players.Remove(player);

            Logs.Log($"[LobbyService] Player {player.PlayerIndex + 1} left the lobby.");

            OnPlayerLeft?.Invoke(player);
        }

        public void RemovePlayer(PlayerManager player)
        {
            if (!player)
                return;

            UnregisterPlayer(player);

            var deviceService = ServiceManager.Instance?.Get<DeviceService>();

            if (player.PlayerInput)
                deviceService?.UnregisterPlayerInput(player.PlayerInput);

            player.SelfDestroy();

            Logs.Log($"[LobbyService] Player {player.PlayerIndex + 1} removed from the lobby.");
        }

        public void ClearPlayers(bool destroyPlayerObjects = true)
        {
            if (Players.Count == 0)
                return;

            Logs.Log($"[LobbyService] Clearing {Players.Count} player(s). DestroyObjects={destroyPlayerObjects}");

            var playersToClear = new List<PlayerManager>(Players);
            Players.Clear();

            var deviceService = ServiceManager.Instance?.Get<DeviceService>();

            for (int i = 0; i < playersToClear.Count; i++)
            {
                var player = playersToClear[i];

                if (!player)
                    continue;

                UnbindPlayerEvents(player);

                if (player.PlayerInput)
                    deviceService?.UnregisterPlayerInput(player.PlayerInput);

                OnPlayerLeft?.Invoke(player);

                if (destroyPlayerObjects)
                    Object.Destroy(player.gameObject);
            }
        }

        private void UnbindPlayerEvents(PlayerManager player)
        {
            if (!player)
                return;

            player.OnPlayerInitialized -= HandlePlayerInitialized;
            player.OnPlayerDestroyed -= HandlePlayerDestroyed;
        }

        private void HandlePlayerInitialized(PlayerManager player)
        {
        }

        private void HandlePlayerDestroyed(PlayerManager player)
        {
            UnregisterPlayer(player);
        }
    }
}