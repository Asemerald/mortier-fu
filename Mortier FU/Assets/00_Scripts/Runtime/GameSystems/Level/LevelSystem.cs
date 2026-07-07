using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MortierFu
{
    public class LevelSystem : IGameSystem
    {
        private const string k_arenaMapsLabel = "ArenaMaps";
        private const string k_raceMapsLabel = "RaceMaps";

        private const string k_editorOverrideArenaMapAddress = "OverrideArenaMapAddress";
        private const string k_editorOverrideRaceMapAddress = "OverrideRaceMapAddress";

        private AsyncOperationHandle<SO_LevelSettings> _settingsHandle;

        public SO_LevelSettings Settings => _settingsHandle.Result;
        public CameraMapConfig CurrentCameraMapConfig;

        private List<IResourceLocation> _arenaMapLocations;
        private List<IResourceLocation> _raceMapLocations;

        private readonly Dictionary<string, int> _arenaMapCooldowns = new();
        private readonly Dictionary<string, int> _raceMapCooldowns = new();

        private AsyncOperationHandle<SceneInstance> _mapHandle;

        private CameraSystem _cameraSystem;
        private LevelReporter _boundReporter;
        private Transform _fallbackTransform;

        public bool IsInitialized { get; set; }

        public UniTask LoadRaceMap(bool useTransition = false, TransitionColor color = TransitionColor.Blue)
        {
            return LoadMapAsync(
                _raceMapLocations,
                _raceMapCooldowns,
                arenaMode: false,
                editorOverrideKey: k_editorOverrideRaceMapAddress,
                debugMapTypeName: "race",
                useTransition,
                color
            );
        }

        public UniTask LoadArenaMap(bool useTransition = false, TransitionColor color = TransitionColor.Blue)
        {
            return LoadMapAsync(
                _arenaMapLocations,
                _arenaMapCooldowns,
                arenaMode: true,
                editorOverrideKey: k_editorOverrideArenaMapAddress,
                debugMapTypeName: "arena",
                useTransition,
                color
            );
        }

        private async UniTask LoadMapAsync(List<IResourceLocation> mapLocations, Dictionary<string, int> mapCooldowns,
            bool arenaMode, string editorOverrideKey, string debugMapTypeName, bool useTransition,
            TransitionColor color)
        {
            await FinishUnfinishedBusiness();

            bool transitionStarted = false;

            try
            {
                if (useTransition)
                {
                    await StartTransitionAsync(color);
                    transitionStarted = true;
                }

                await UnloadCurrentMap();

                SetCameraArenaMode(arenaMode);

                bool editorOverrideLoaded = await TryLoadEditorOverrideMapAsync(
                    editorOverrideKey,
                    debugMapTypeName
                );

                if (editorOverrideLoaded)
                    return;

                IResourceLocation mapLocation = GetRandomMapLocation(
                    mapLocations,
                    mapCooldowns,
                    debugMapTypeName
                );

                if (mapLocation is null)
                    return;

                bool loaded = await LoadAddressableMapAsync(
                    mapLocation,
                    debugMapTypeName
                );

                if (!loaded)
                    return;

                ApplyLoadedMapData();
            }
            finally
            {
                if (transitionStarted)
                {
                    EndTransition();
                }
            }
        }

        private async UniTask<bool> TryLoadEditorOverrideMapAsync(string editorOverrideKey, string debugMapTypeName)
        {
#if UNITY_EDITOR
            string sceneKey = EditorPrefs.GetString(editorOverrideKey, "");

            if (string.IsNullOrEmpty(sceneKey))
                return false;

            IResourceLocation sceneLocation = null;

            var locationsHandle = Addressables.LoadResourceLocationsAsync(sceneKey);

            try
            {
                await locationsHandle;

                if (locationsHandle.Status != AsyncOperationStatus.Succeeded ||
                    locationsHandle.Result == null ||
                    locationsHandle.Result.Count == 0)
                {
                    if (IsDebugEnabled)
                    {
                        Logs.LogWarning(
                            $"[LevelSystem] Debug {debugMapTypeName} scene key not found in Addressables: {sceneKey}");
                    }

                    return false;
                }

                for (int i = 0; i < locationsHandle.Result.Count; i++)
                {
                    IResourceLocation location = locationsHandle.Result[i];

                    if (!IsSceneLocation(location)) continue;
                    sceneLocation = location;
                    break;
                }

                if (sceneLocation is null)
                {
                    Logs.LogError(
                        $"[LevelSystem] Debug {debugMapTypeName} key found, but no valid scene location was found for: {sceneKey}");
                    return false;
                }

                bool loaded = await LoadAddressableMapAsync(
                    sceneLocation,
                    $"debug {debugMapTypeName}"
                );

                if (!loaded)
                    return false;

                ApplyLoadedMapData();

                if (IsDebugEnabled)
                {
                    Logs.Log(
                        $"[LevelSystem] Enforced debug {debugMapTypeName} scene: {DescribeSceneKey(sceneLocation)}");
                }

                return true;
            }
            finally
            {
                if (locationsHandle.IsValid())
                {
                    Addressables.Release(locationsHandle);
                }
            }
#else
    return false;
#endif
        }

        private async UniTask<bool> LoadAddressableMapAsync(object sceneKey, string debugMapTypeName)
        {
            if (sceneKey is null)
            {
                Logs.LogError($"[LevelSystem] Cannot load {debugMapTypeName} map: scene key is null.");
                return false;
            }

            object loadKey = sceneKey;

            if (sceneKey is IResourceLocation location)
            {
                loadKey = location.PrimaryKey;

                if (IsDebugEnabled)
                {
                    Logs.Log(
                        $"[LevelSystem] Loading {debugMapTypeName} map from location. {DescribeSceneKey(location)}. LoadKey='{loadKey}'");
                }
            }

            try
            {
                _mapHandle = Addressables.LoadSceneAsync(
                    loadKey,
                    LoadSceneMode.Additive,
                    SceneReleaseMode.ReleaseSceneWhenSceneUnloaded
                );

                await _mapHandle;
            }
            catch (Exception e)
            {
                Logs.LogError(
                    $"[LevelSystem] Exception while loading {debugMapTypeName} map with key '{DescribeSceneKey(sceneKey)}' and loadKey '{loadKey}': {e.Message}");
                _mapHandle = default;
                return false;
            }

            if (_mapHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Logs.LogError(
                    $"[LevelSystem] Failed to load {debugMapTypeName} map with key '{DescribeSceneKey(sceneKey)}' and loadKey '{loadKey}': {_mapHandle.OperationException?.Message}");
                _mapHandle = default;
                return false;
            }

            if (IsDebugEnabled)
            {
                Logs.Log($"[LevelSystem] Loaded {debugMapTypeName} map: {_mapHandle.Result.Scene.name}");
            }

            return true;
        }

        public async UniTask UnloadCurrentMap()
        {
            await FinishUnfinishedBusiness();

            if (!_mapHandle.IsValid())
            {
                ClearCurrentMapData();
                return;
            }

            if (_mapHandle.Status == AsyncOperationStatus.Succeeded)
            {
                await Addressables.UnloadSceneAsync(_mapHandle);
            }
            else
            {
                Addressables.Release(_mapHandle);
            }

            _mapHandle = default;

            ClearCurrentMapData();

            if (IsDebugEnabled)
            {
                Logs.Log("[LevelSystem] Current map unloaded.");
            }
        }

        public bool IsRaceMap()
        {
            if (BoundReporter == null)
                return false;

            return BoundReporter.IsRaceMap;
        }

        public Transform GetWinnerSpawnPoint()
        {
            if (BoundReporter == null)
                return FallbackTransform;

            return BoundReporter.WinnerSpawnPoint ?? FallbackTransform;
        }

        public Transform GetRoundWinnerSpawnPoint()
        {
            if (BoundReporter == null)
                return FallbackTransform;

            return BoundReporter.RoundWinnerSpawnPoint ?? FallbackTransform;
        }

        public Transform GetSpawnPoint(int index)
        {
            if (BoundReporter == null)
                return FallbackTransform;

            if (index < 0)
            {
                if (IsDebugEnabled)
                    Logs.LogWarning("[LevelSystem] Trying to get a spawn point with a negative index.");

                return FallbackTransform;
            }

            if (BoundReporter.SpawnPoints != null && BoundReporter.SpawnPoints.Length != 0)
                return BoundReporter.SpawnPoints[index % BoundReporter.SpawnPoints.Length];
            
            if (IsDebugEnabled)
                Logs.LogWarning("[LevelSystem] No spawn points available on current LevelReporter.");

            return FallbackTransform;

        }

        public void PopulateAugmentPoints(Vector3[] outPoints)
        {
            if (outPoints == null)
                return;

            if (BoundReporter == null)
            {
                for (int i = 0; i < outPoints.Length; i++)
                {
                    outPoints[i] = Vector3.zero;
                }

                return;
            }

            for (int i = 0; i < outPoints.Length; i++)
            {
                outPoints[i] = BoundReporter.GetAugmentPoint(outPoints.Length, i);
            }
        }

        public Transform GetAugmentPivot()
        {
            return BoundReporter != null
                ? BoundReporter.AugmentPivot ?? FallbackTransform
                : FallbackTransform;
        }

        public void BindReporter(LevelReporter reporter)
        {
            if (reporter == null)
            {
                if (IsDebugEnabled)
                {
                    Logs.LogWarning("[LevelSystem] Trying to bind a null LevelReporter.");
                }

                return;
            }

            _boundReporter = reporter;

            if (IsDebugEnabled)
            {
                Logs.Log("[LevelSystem] Successfully bound a new LevelReporter.");
            }
        }

        private void ApplyLoadedMapData()
        {
            var reporter = BoundReporter;

            if (reporter == null)
            {
                Logs.LogWarning("[LevelSystem] Loaded map has no LevelReporter.");
                CurrentCameraMapConfig = default;
                return;
            }

            CurrentCameraMapConfig = reporter.CameraConfig;
        }

        private void ClearCurrentMapData()
        {
            _boundReporter = null;
            CurrentCameraMapConfig = default;
        }

        private IResourceLocation GetRandomMapLocation(List<IResourceLocation> mapLocations,
            Dictionary<string, int> mapCooldowns, string debugMapTypeName)
        {
            if (mapLocations == null || mapLocations.Count == 0)
            {
                Logs.LogError($"[LevelSystem] No {debugMapTypeName} map locations available.");
                return null;
            }

            int unavailabilityRounds = GetMapUnavailabilityRounds();

            if (unavailabilityRounds <= 0)
                return mapLocations.RandomElement();

            SynchronizeMapCooldowns(mapLocations, mapCooldowns);

            var availableMaps = new List<IResourceLocation>();

            for (int i = 0; i < mapLocations.Count; i++)
            {
                var location = mapLocations[i];
                string key = GetMapCooldownKey(location);

                bool isUnavailable =
                    mapCooldowns.TryGetValue(key, out int remainingRounds) &&
                    remainingRounds > 0;

                if (!isUnavailable)
                    availableMaps.Add(location);
            }

            if (availableMaps.Count == 0)
            {
                Logs.LogWarning(
                    $"[LevelSystem] All {debugMapTypeName} maps are currently unavailable. " +
                    "Selecting the map with the lowest remaining cooldown."
                );

                availableMaps = GetMapsWithLowestCooldown(mapLocations, mapCooldowns);
            }

            var selectedMap = availableMaps.RandomElement();
            string selectedKey = GetMapCooldownKey(selectedMap);

            TickMapCooldowns(mapCooldowns, selectedKey);

            mapCooldowns[selectedKey] = unavailabilityRounds;

            if (IsDebugEnabled)
            {
                Logs.Log(
                    $"[LevelSystem] Selected {debugMapTypeName} map: {DescribeSceneKey(selectedMap)}. " +
                    $"Unavailable for next {unavailabilityRounds} {debugMapTypeName} selections."
                );
            }

            return selectedMap;
        }

        private int GetMapUnavailabilityRounds()
        {
            if (!_settingsHandle.IsValid() ||
                _settingsHandle.Status != AsyncOperationStatus.Succeeded ||
                _settingsHandle.Result == null)
            {
                return 0;
            }

            return Mathf.Max(0, Settings.NbOfUnavailabilityRounds);
        }

        private static string GetMapCooldownKey(IResourceLocation location)
        {
            if (location is null)
                return string.Empty;

            if (!string.IsNullOrEmpty(location.PrimaryKey))
                return location.PrimaryKey;

            if (!string.IsNullOrEmpty(location.InternalId))
                return location.InternalId;

            return location.ToString();
        }

        private static void TickMapCooldowns(Dictionary<string, int> mapCooldowns, string selectedKey)
        {
            if (mapCooldowns == null || mapCooldowns.Count == 0)
                return;

            var keys = new List<string>(mapCooldowns.Keys);

            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];

                if (key == selectedKey)
                    continue;

                mapCooldowns[key]--;

                if (mapCooldowns[key] <= 0)
                    mapCooldowns.Remove(key);
            }
        }

        private static void SynchronizeMapCooldowns(List<IResourceLocation> mapLocations, Dictionary<string, int> mapCooldowns)
        {
            if (mapCooldowns == null || mapCooldowns.Count == 0)
                return;

            var validKeys = new HashSet<string>();

            for (int i = 0; i < mapLocations.Count; i++)
            {
                validKeys.Add(GetMapCooldownKey(mapLocations[i]));
            }

            var cooldownKeys = new List<string>(mapCooldowns.Keys);

            for (int i = 0; i < cooldownKeys.Count; i++)
            {
                string key = cooldownKeys[i];

                if (!validKeys.Contains(key))
                    mapCooldowns.Remove(key);
            }
        }

        private static List<IResourceLocation> GetMapsWithLowestCooldown(List<IResourceLocation> mapLocations, Dictionary<string, int> mapCooldowns)
        {
            var result = new List<IResourceLocation>();

            int lowestCooldown = int.MaxValue;

            for (int i = 0; i < mapLocations.Count; i++)
            {
                var location = mapLocations[i];
                string key = GetMapCooldownKey(location);

                int cooldown = mapCooldowns.TryGetValue(key, out int remainingRounds)
                    ? remainingRounds
                    : 0;

                if (cooldown < lowestCooldown)
                {
                    lowestCooldown = cooldown;
                    result.Clear();
                    result.Add(location);
                }
                else if (cooldown == lowestCooldown)
                {
                    result.Add(location);
                }
            }

            return result;
        }

        private async UniTask FinishUnfinishedBusiness()
        {
            if (!_mapHandle.IsValid() || _mapHandle.IsDone)
                return;

            await _mapHandle;

            if (_mapHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Logs.LogError($"[LevelSystem] Previous map load failed: {_mapHandle.OperationException?.Message}");
            }
        }

        private async UniTask<List<IResourceLocation>> LoadMapsByLabel(string label)
        {
            var handle = Addressables.LoadResourceLocationsAsync(
                label,
                typeof(SceneInstance)
            );

            try
            {
                await handle;

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Logs.LogError(
                        $"[LevelSystem] Failed to load map locations for label '{label}': {handle.OperationException?.Message}");
                    return new List<IResourceLocation>();
                }

                var locations = new List<IResourceLocation>();

                for (int i = 0; i < handle.Result.Count; i++)
                {
                    var location = handle.Result[i];

                    if (!IsSceneLocation(location))
                    {
                        if (IsDebugEnabled)
                        {
                            Logs.LogWarning(
                                $"[LevelSystem] Ignored non-scene location for label '{label}': {DescribeSceneKey(location)}");
                        }

                        continue;
                    }

                    locations.Add(location);

                    if (IsDebugEnabled)
                    {
                        Logs.Log(
                            $"[LevelSystem] Registered scene location for label '{label}': {DescribeSceneKey(location)}");
                    }
                }

                if (locations.Count == 0)
                {
                    Logs.LogError(
                        $"[LevelSystem] No valid SceneInstance locations found for label '{label}'. Check Addressables setup.");
                }

                return locations;
            }
            finally
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
        }

        private async UniTask StartTransitionAsync(TransitionColor color)
        {
            if (TransitionManager.Instance == null)
            {
                Logs.LogWarning("[LevelSystem] TransitionManager is missing. Cannot start transition.");
                return;
            }

            await TransitionManager.Instance.StartTransitionAsync(color);
        }

        private void EndTransition()
        {
            if (TransitionManager.Instance == null)
            {
                Logs.LogWarning("[LevelSystem] TransitionManager is missing. Cannot end transition.");
                return;
            }

            TransitionManager.Instance.EndTransition().Forget();
        }

        private void SetCameraArenaMode(bool arenaMode)
        {
            if (_cameraSystem == null || _cameraSystem.Controller == null)
            {
                Logs.LogWarning("[LevelSystem] CameraSystem or camera controller is missing.");
                return;
            }

            _cameraSystem.Controller.SetArenaMode(arenaMode);
        }

        private LevelReporter BoundReporter
        {
            get
            {
                if (_boundReporter != null)
                    return _boundReporter;

                if (IsDebugEnabled)
                {
                    Logs.LogWarning("[LevelSystem] No LevelReporter bound to the LevelSystem.");
                }

                var reporter = Object.FindFirstObjectByType<LevelReporter>();

                if (reporter != null)
                {
                    if (IsDebugEnabled)
                    {
                        Logs.LogWarning("[LevelSystem] Found a LevelReporter in the scene. Falling back to it.");
                    }

                    _boundReporter = reporter;
                    return _boundReporter;
                }

                return null;
            }
        }

        public Transform FallbackTransform
        {
            get
            {
                if (_fallbackTransform == null)
                {
                    _fallbackTransform = new GameObject("FallbackSpawnPoint").transform;
                }

                return _fallbackTransform;
            }
        }

        private bool IsDebugEnabled
        {
            get
            {
                return _settingsHandle.IsValid() &&
                       _settingsHandle.Status == AsyncOperationStatus.Succeeded &&
                       _settingsHandle.Result != null &&
                       _settingsHandle.Result.EnableDebug;
            }
        }

        public async UniTask OnInitialize()
        {
            _cameraSystem = SystemManager.Instance.Get<CameraSystem>();

            if (_cameraSystem == null)
            {
                Logs.LogWarning("[LevelSystem] No CameraSystem found.");
            }

            _settingsHandle = await SystemManager.Config.LevelSettings.LazyLoadAssetRef();

            _arenaMapLocations = await LoadMapsByLabel(k_arenaMapsLabel);
            _raceMapLocations = await LoadMapsByLabel(k_raceMapsLabel);
        }

        private static bool IsSceneLocation(IResourceLocation location)
        {
            return location is not null &&
                   location.ResourceType == typeof(SceneInstance);
        }

        private static string DescribeSceneKey(object sceneKey)
        {
            if (sceneKey is null)
                return "<null>";

            if (sceneKey is IResourceLocation location)
            {
                return
                    $"PrimaryKey='{location.PrimaryKey}', InternalId='{location.InternalId}', ResourceType='{location.ResourceType}'";
            }

            return sceneKey.ToString();
        }

        public void Dispose()
        {
            if (_settingsHandle.IsValid())
            {
                Addressables.Release(_settingsHandle);
            }

            _arenaMapLocations?.Clear();
            _raceMapLocations?.Clear();

            _arenaMapCooldowns.Clear();
            _raceMapCooldowns.Clear();

            ClearCurrentMapData();

            if (_fallbackTransform == null) return;
            Object.Destroy(_fallbackTransform.gameObject);
            _fallbackTransform = null;
        }
    }
}