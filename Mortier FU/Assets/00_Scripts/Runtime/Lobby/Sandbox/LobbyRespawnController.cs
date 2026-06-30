using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbyRespawnController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LobbySandboxController _sandboxController;

        [Header("Respawn")]
        [SerializeField] private bool _enableRespawn = true;
        [SerializeField] private float _respawnDelay = 1.0f;

        private readonly Dictionary<PlayerManager, RespawnBinding> _bindings = new();
        private readonly HashSet<PlayerManager> _respawnInProgress = new();

        private sealed class RespawnBinding
        {
            public PlayerCharacter Character;
            public Action<object> DeathHandler;
        }

        private void Awake()
        {
            if (!_sandboxController)
            {
                _sandboxController = GetComponent<LobbySandboxController>();
            }
        }

        private void OnEnable()
        {
            if (!_sandboxController)
            {
                Logs.LogError("[LobbyRespawnController] Missing LobbySandboxController reference.");
                return;
            }

            _sandboxController.OnPlayerSpawned += RegisterPlayer;

            var spawnedPlayers = _sandboxController.GetSpawnedPlayers();

            for (int i = 0; i < spawnedPlayers.Count; i++)
            {
                RegisterPlayer(spawnedPlayers[i]);
            }
        }

        private void OnDisable()
        {
            if (_sandboxController)
            {
                _sandboxController.OnPlayerSpawned -= RegisterPlayer;
            }

            UnregisterAllPlayers();
            _respawnInProgress.Clear();
        }

        private void RegisterPlayer(PlayerManager player)
        {
            if (!_enableRespawn)
                return;

            if (!player)
                return;

            PlayerCharacter character = player.Character;

            if (!character)
            {
                Logs.LogWarning($"[LobbyRespawnController] Cannot register Player {player.PlayerIndex + 1}: character is null.");
                return;
            }

            if (character.Health == null)
            {
                Logs.LogWarning($"[LobbyRespawnController] Cannot register Player {player.PlayerIndex + 1}: health is null.");
                return;
            }

            UnregisterPlayer(player);

            Action<object> deathHandler = _ => HandlePlayerDeath(player);

            character.Health.OnDeath += deathHandler;

            _bindings[player] = new RespawnBinding
            {
                Character = character,
                DeathHandler = deathHandler
            };

            Logs.Log($"[LobbyRespawnController] Registered respawn for Player {player.PlayerIndex + 1}.");
        }

        private void UnregisterPlayer(PlayerManager player)
        {
            if (!player)
                return;

            if (!_bindings.TryGetValue(player, out RespawnBinding binding))
                return;

            if (binding.Character&&
                binding.Character.Health != null &&
                binding.DeathHandler != null)
            {
                binding.Character.Health.OnDeath -= binding.DeathHandler;
            }

            _bindings.Remove(player);
        }

        private void UnregisterAllPlayers()
        {
            var players = new List<PlayerManager>(_bindings.Keys);

            for (int i = 0; i < players.Count; i++)
            {
                UnregisterPlayer(players[i]);
            }

            _bindings.Clear();
        }

        private void HandlePlayerDeath(PlayerManager player)
        {
            if (!_enableRespawn)
                return;

            if (!player)
                return;

            if (_respawnInProgress.Contains(player))
                return;

            RespawnAfterDelayAsync(player, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid RespawnAfterDelayAsync(PlayerManager player, CancellationToken cancellationToken)
        {
            _respawnInProgress.Add(player);

            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_respawnDelay),
                    cancellationToken: cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                _respawnInProgress.Remove(player);
                return;
            }

            _respawnInProgress.Remove(player);

            RespawnPlayer(player);
        }

        private void RespawnPlayer(PlayerManager player)
        {
            if (!player)
                return;

            if (!_sandboxController)
                return;

            if (!_sandboxController.TryGetSpawnPoint(player.PlayerIndex, out Transform spawnPoint))
            {
                Logs.LogError($"[LobbyRespawnController] Cannot respawn Player {player.PlayerIndex + 1}: missing spawn point.");
                return;
            }

            PlayerCharacter character = player.Character;

            if (!character)
            {
                Logs.LogError($"[LobbyRespawnController] Cannot respawn Player {player.PlayerIndex + 1}: character is null.");
                return;
            }

            character.RespawnAt(spawnPoint.position, spawnPoint.rotation);

            _sandboxController.ApplyCurrentContextToPlayer(player);

            Logs.Log($"[LobbyRespawnController] Respawned Player {player.PlayerIndex + 1}.");
        }
    }
}