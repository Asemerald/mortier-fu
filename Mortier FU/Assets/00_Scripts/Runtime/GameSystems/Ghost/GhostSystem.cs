using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace MortierFu
{
    public sealed class GhostSystem : IGameSystem
    {
        private const string k_ghostSettingsAddress = "DA_GhostSettings";

        private AsyncOperationHandle<SO_GhostSettings> _settingsHandle;

        private readonly Dictionary<PlayerManager, PlayerGhostPawn> _activeGhosts = new();
        private readonly Dictionary<PlayerManager, CancellationTokenSource> _pendingGhostSpawns = new();

        private EventBinding<EventPlayerDeath> _playerDeathBinding;
        private EventBinding<TriggerEndRound> _endRoundBinding;

        private readonly Dictionary<PlayerManager, List<GameObject>> _spawnedPropsByOwner = new();

        private GhostSpawnResolver _spawnResolver;
        
        private Transform _ghostPropsParent;
        private Transform _ghostParent;

        public bool IsInitialized { get; set; }

        public SO_GhostSettings Settings =>
            _settingsHandle.IsValid() ? _settingsHandle.Result : null;

        public async UniTask OnInitialize()
        {
            _settingsHandle = await AddressablesUtils.LazyLoadAsset<SO_GhostSettings>(k_ghostSettingsAddress);
            if (!Settings)
            {
                Logs.LogError("[GhostSystem] Ghost settings are missing.");
                return;
            }

            _spawnResolver = new GhostSpawnResolver(Settings, SystemManager.Instance.Get<LevelSystem>());
            
            _ghostParent = new GameObject("Ghosts").transform;
            _ghostPropsParent = new GameObject("Ghost Spawned Props").transform;
            
            _playerDeathBinding = new EventBinding<EventPlayerDeath>(OnPlayerDeath);
            _endRoundBinding = new EventBinding<TriggerEndRound>(OnEndRound);

            EventBus<EventPlayerDeath>.Register(_playerDeathBinding);
            EventBus<TriggerEndRound>.Register(_endRoundBinding);

            Logs.Log("[GhostSystem] Initialized.");
        }

        private void OnPlayerDeath(EventPlayerDeath evt)
        {
            if (!evt.Character || !evt.Character.Owner)
                return;

            PlayerCharacter deadCharacter = evt.Character;
            PlayerManager owner = deadCharacter.Owner;

            CancelPendingGhostSpawn(owner);
            ClearGhost(owner);

            GhostSpawnResult spawn = _spawnResolver.Resolve(deadCharacter, evt.Context);
            Vector3 spawnPosition = spawn.Position;
            Quaternion spawnRotation = spawn.Rotation;

            CancellationTokenSource cts = new();
            _pendingGhostSpawns[owner] = cts;

            SpawnGhostAfterDelayAsync(
                owner,
                deadCharacter,
                spawnPosition,
                spawnRotation,
                cts.Token
            ).Forget();
        }

        private async UniTaskVoid SpawnGhostAfterDelayAsync(PlayerManager owner, PlayerCharacter sourceCharacter, Vector3 spawnPosition, Quaternion spawnRotation, CancellationToken cancellationToken)
        {
            try
            {
                float delay = Settings ? Mathf.Max(0f, Settings.SpawnDelay) : 2f;

                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                if (!owner || !sourceCharacter)
                    return;

                if (sourceCharacter.Health == null || sourceCharacter.Health.IsAlive)
                    return;

                SpawnGhost(
                    owner,
                    sourceCharacter,
                    spawnPosition,
                    spawnRotation
                );
            }
            catch (OperationCanceledException)
            {
                // Noop
            }
            finally
            {
                if (owner)
                    _pendingGhostSpawns.Remove(owner);
            }
        }

        private void SpawnGhost(PlayerManager owner, PlayerCharacter sourceCharacter, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            if (!Settings)
            {
                Logs.LogError("[GhostSystem] Cannot spawn ghost because settings are missing.");
                return;
            }

            if (!Settings.GhostPawnPrefab)
            {
                Logs.LogError("[GhostSystem] Cannot spawn ghost because GhostPawnPrefab is missing.");
                return;
            }

            ClearGhost(owner);

            PlayerGhostPawn ghost = Object.Instantiate(Settings.GhostPawnPrefab, spawnPosition, spawnRotation, _ghostParent);

            ghost.Initialize(
                owner,
                sourceCharacter,
                Settings
            );

            ghost.Teleport(
                spawnPosition,
                spawnRotation
            );

            _activeGhosts[owner] = ghost;

            owner.SetControlContext(PlayerControlContext.RoundGhost);
            owner.SetActivePawn(ghost);
            
            Logs.Log($"[GhostSystem] Spawned ghost for Player {owner.PlayerIndex + 1}.");
        }

        private void OnEndRound(TriggerEndRound evt)
        {
            ClearAllGhosts();
            ClearAllSpawnedProps();
        }

        private void CancelPendingGhostSpawn(PlayerManager owner)
        {
            if (!owner)
                return;

            if (!_pendingGhostSpawns.TryGetValue(owner, out CancellationTokenSource cts))
                return;

            cts.Cancel();
            cts.Dispose();

            _pendingGhostSpawns.Remove(owner);
        }

        private void CancelAllPendingGhostSpawns()
        {
            foreach (var kvp in _pendingGhostSpawns)
            {
                kvp.Value.Cancel();
                kvp.Value.Dispose();
            }

            _pendingGhostSpawns.Clear();
        }

        private void ClearGhost(PlayerManager owner)
        {
            if (!owner)
                return;

            if (!_activeGhosts.TryGetValue(owner, out PlayerGhostPawn ghost))
                return;

            owner.ClearActivePawn(ghost);

            if (ghost)
                Object.Destroy(ghost.gameObject);

            _activeGhosts.Remove(owner);
        }

        private void ClearAllGhosts()
        {
            CancelAllPendingGhostSpawns();

            var owners = new List<PlayerManager>(_activeGhosts.Keys);

            for (int i = 0; i < owners.Count; i++)
            {
                ClearGhost(owners[i]);
            }
        }
        
        public void RegisterSpawnedProp(PlayerManager owner, GameObject prop)
        {
            if (!owner || !prop)
                return;

            if (_ghostPropsParent)
                prop.transform.SetParent(_ghostPropsParent, true);

            if (!_spawnedPropsByOwner.TryGetValue(owner, out var props))
            {
                props = new List<GameObject>();
                _spawnedPropsByOwner.Add(owner, props);
            }

            props.Add(prop);
        }

        private void ClearSpawnedProps(PlayerManager owner)
        {
            if (!owner)
                return;

            if (!_spawnedPropsByOwner.TryGetValue(owner, out var props))
                return;

            DestroySpawnedProps(props);
            _spawnedPropsByOwner.Remove(owner);
        }

        private void ClearAllSpawnedProps()
        {
            foreach (var kvp in _spawnedPropsByOwner)
            {
                DestroySpawnedProps(kvp.Value);
            }

            _spawnedPropsByOwner.Clear();
        }

        private static void DestroySpawnedProps(List<GameObject> props)
        {
            if (props == null)
                return;

            for (int i = props.Count - 1; i >= 0; i--)
            {
                if (props[i])
                    Object.Destroy(props[i]);
            }

            props.Clear();
        }

        public void Dispose()
        {
            if (_playerDeathBinding != null)
            {
                EventBus<EventPlayerDeath>.Deregister(_playerDeathBinding);
                _playerDeathBinding = null;
            }

            if (_endRoundBinding != null)
            {
                EventBus<TriggerEndRound>.Deregister(_endRoundBinding);
                _endRoundBinding = null;
            }

            ClearAllGhosts();
            ClearAllSpawnedProps();

            if (_ghostParent)
            {
                Object.Destroy(_ghostParent.gameObject);
                _ghostParent = null;
            }

            if (_ghostPropsParent)
            {
                Object.Destroy(_ghostPropsParent.gameObject);
                _ghostPropsParent = null;
            }

            if (_settingsHandle.IsValid())
                Addressables.Release(_settingsHandle);

            Logs.Log("[GhostSystem] Disposed.");
        }
    }
}