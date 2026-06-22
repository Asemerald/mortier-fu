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

        private AsyncOperationHandle<SceneInstance> _mapHandle;

        private CameraSystem _cameraSystem;
        private LevelReporter _boundReporter;
        private Transform _fallbackTransform;

        public bool IsInitialized { get; set; }

        public UniTask LoadRaceMap(
            bool useTransition = false,
            TransitionColor color = TransitionColor.Blue
        )
        {
            return LoadMapAsync(
                _raceMapLocations,
                arenaMode: false,
                editorOverrideKey: k_editorOverrideRaceMapAddress,
                debugMapTypeName: "race",
                useTransition,
                color
            );
        }

        public UniTask LoadArenaMap(
            bool useTransition = false,
            TransitionColor color = TransitionColor.Blue
        )
        {
            return LoadMapAsync(
                _arenaMapLocations,
                arenaMode: true,
                editorOverrideKey: k_editorOverrideArenaMapAddress,
                debugMapTypeName: "arena",
                useTransition,
                color
            );
        }

        private async UniTask LoadMapAsync(
            List<IResourceLocation> mapLocations,
            bool arenaMode,
            string editorOverrideKey,
            string debugMapTypeName,
            bool useTransition,
            TransitionColor color
        )
        {
            await FinishUnfinishedBusiness();

            if (useTransition)
            {
                await StartTransitionAsync(color);
            }

            await UnloadCurrentMap();

            SetCameraArenaMode(arenaMode);

            bool editorOverrideLoaded = await TryLoadEditorOverrideMapAsync(
                editorOverrideKey,
                debugMapTypeName,
                useTransition
            );

            if (editorOverrideLoaded)
                return;

            var mapLocation = GetRandomMapLocation(mapLocations, debugMapTypeName);

            if (mapLocation == null)
            {
                if (useTransition)
                {
                    EndTransition();
                }

                return;
            }

            bool loaded = await LoadAddressableMapAsync(
                mapLocation,
                debugMapTypeName
            );

            if (useTransition)
            {
                EndTransition();
            }

            if (!loaded)
                return;

            ApplyLoadedMapData();
        }

        private async UniTask<bool> TryLoadEditorOverrideMapAsync(
            string editorOverrideKey,
            string debugMapTypeName,
            bool useTransition
        )
        {
#if UNITY_EDITOR
            string sceneKey = EditorPrefs.GetString(editorOverrideKey, "");

            if (string.IsNullOrEmpty(sceneKey))
                return false;

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
                        Logs.LogWarning($"[LevelSystem] Debug {debugMapTypeName} scene key not found in Addressables: {sceneKey}");
                    }

                    return false;
                }
            }
            finally
            {
                if (locationsHandle.IsValid())
                {
                    Addressables.Release(locationsHandle);
                }
            }

            bool loaded = await LoadAddressableMapAsync(
                sceneKey,
                $"debug {debugMapTypeName}"
            );

            if (!loaded)
                return false;

            ApplyLoadedMapData();

            if (useTransition)
            {
                EndTransition();
            }

            if (IsDebugEnabled)
            {
                Logs.Log($"[LevelSystem] Enforced debug {debugMapTypeName} scene: {sceneKey}");
            }

            return true;
#else
            return false;
#endif
        }

        private async UniTask<bool> LoadAddressableMapAsync(
            object sceneKey,
            string debugMapTypeName
        )
        {
            _mapHandle = Addressables.LoadSceneAsync(
                sceneKey,
                LoadSceneMode.Additive,
                SceneReleaseMode.ReleaseSceneWhenSceneUnloaded
            );

            await _mapHandle;

            if (_mapHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Logs.LogError($"[LevelSystem] Failed to load {debugMapTypeName} map: {_mapHandle.OperationException?.Message}");
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

        public bool GetCurrentLevelScene(out Scene scene)
        {
            if (_mapHandle.IsValid() && _mapHandle.IsDone)
            {
                scene = _mapHandle.Result.Scene;
                return scene.IsValid();
            }

            scene = default;
            return false;
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
                {
                    Logs.LogWarning("[LevelSystem] Trying to get a spawn point with a negative index.");
                }

                return FallbackTransform;
            }

            return BoundReporter.SpawnPoints[index % BoundReporter.SpawnPoints.Length];
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

        private IResourceLocation GetRandomMapLocation(
            List<IResourceLocation> mapLocations,
            string debugMapTypeName
        )
        {
            if (mapLocations == null || mapLocations.Count == 0)
            {
                Logs.LogError($"[LevelSystem] No {debugMapTypeName} map locations available.");
                return null;
            }

            return mapLocations.RandomElement();
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
                    Logs.LogError($"[LevelSystem] Failed to load map locations for label '{label}': {handle.OperationException?.Message}");
                    return new List<IResourceLocation>();
                }

                var locations = new List<IResourceLocation>(handle.Result);

                if (IsDebugEnabled)
                {
                    Logs.Log($"[LevelSystem] Loaded {locations.Count} map locations for label '{label}'.");
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

        public void Dispose()
        {
            if (_settingsHandle.IsValid())
            {
                Addressables.Release(_settingsHandle);
            }

            _arenaMapLocations?.Clear();
            _raceMapLocations?.Clear();

            ClearCurrentMapData();

            if (_fallbackTransform != null)
            {
                Object.Destroy(_fallbackTransform.gameObject);
                _fallbackTransform = null;
            }
        }
    }
}