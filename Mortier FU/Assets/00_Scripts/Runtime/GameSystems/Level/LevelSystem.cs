using UnityEngine;
using MortierFu.Shared;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MortierFu
{
    public class LevelSystem : IGameSystem
    {
        private AsyncOperationHandle<SO_LevelSettings> _settingsHandle;
        public SO_LevelSettings Settings => _settingsHandle.Result;
        
        private List<IResourceLocation> _arenaMapLocations;
        private List<IResourceLocation> _raceMapLocations;
        
        // Used to track and unload the current loaded map
        private AsyncOperationHandle<SceneInstance> _mapHandle;

        private const string k_arenaMapsLabel = "ArenaMaps";
        private const string k_raceMapsLabel = "RaceMaps";

        public async UniTask LoadRaceMap()
        {
            await FinishUnfinishedBusiness();
            await UnloadCurrentMap();

            #if UNITY_EDITOR
            string sceneKey = EditorPrefs.GetString("OverrideRaceMapAddress", "");
            if (!string.IsNullOrEmpty(sceneKey)) {
                var locations = await Addressables.LoadResourceLocationsAsync(sceneKey);
                if (locations.Count > 0) {
                    _mapHandle = Addressables.LoadSceneAsync(sceneKey, LoadSceneMode.Additive, SceneReleaseMode.ReleaseSceneWhenSceneUnloaded);
                    await _mapHandle;
                
                    if(Settings.EnableDebug)
                        Logs.Log($"[LevelSystem]: Enforce the use of the debug scene: {sceneKey}");
                
                    return;
                }
                else 
                {
                    if(Settings.EnableDebug)
                        Logs.LogWarning($"[LevelSystem]: Debug scene key not found in Addressables: {sceneKey}");
                }

            }
            #endif
            
            var map = _raceMapLocations.RandomElement();
            
            _mapHandle = Addressables.LoadSceneAsync(map, LoadSceneMode.Additive, SceneReleaseMode.ReleaseSceneWhenSceneUnloaded);
            await _mapHandle;
            
            if(Settings.EnableDebug)
                Logs.Log($"[LevelSystem]: Random map selected: {_mapHandle.Result.Scene.name} !");
        }

        public async UniTask LoadArenaMap()
        {
            await FinishUnfinishedBusiness();
            await UnloadCurrentMap();
            
            #if UNITY_EDITOR
            string sceneKey = EditorPrefs.GetString("OverrideArenaMapAddress", "");
            if (!string.IsNullOrEmpty(sceneKey)) {
                var locations = await Addressables.LoadResourceLocationsAsync(sceneKey);
                if (locations.Count > 0) {
                    _mapHandle = Addressables.LoadSceneAsync(sceneKey, LoadSceneMode.Additive, SceneReleaseMode.ReleaseSceneWhenSceneUnloaded);
                    await _mapHandle;
                
                    if(Settings.EnableDebug)
                        Logs.Log($"[LevelSystem]: Enforce the use of the debug scene: {sceneKey}");
                
                    return;
                }
                else 
                {
                    if(Settings.EnableDebug)
                        Logs.LogWarning($"[LevelSystem]: Debug scene key not found in Addressables: {sceneKey}");
                }
            }
            #endif
            
            var map = _arenaMapLocations.RandomElement();
            
            _mapHandle = Addressables.LoadSceneAsync(map, LoadSceneMode.Additive, SceneReleaseMode.ReleaseSceneWhenSceneUnloaded);
            await _mapHandle;
            
            if(Settings.EnableDebug)
                Logs.Log($"[LevelSystem]: Random map selected: {_mapHandle.Result.Scene.name} !");
        }
        
        private async UniTask UnloadCurrentMap()
        {
            if (!_mapHandle.IsValid()) return;

            await Addressables.UnloadSceneAsync(_mapHandle);
            _mapHandle = default;
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
        
        public Transform GetSpawnPoint(int index)
        {
            if (BoundReporter == null)
                return FallbackTransform;

            if (index < 0) // || index >= BoundReporter.SpawnPoints.Length
            {
                if(Settings.EnableDebug) 
                    Logs.LogWarning("[LevelSystem]: Trying to get a spawn point which is out of range of the provided list of spawn points by the level reporter !");
                return FallbackTransform;
            }
            
            return BoundReporter.SpawnPoints[index % BoundReporter.SpawnPoints.Length];
        }

        public void PopulateAugmentPoints(Vector3[] outPoints)
        {
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
        
        public Transform GetAugmentPivot() => BoundReporter != null ? BoundReporter.AugmentPivot ?? FallbackTransform : FallbackTransform;
        
        public void BindReporter(LevelReporter reporter)
        {
            if (reporter == null)
            {
                if (Settings.EnableDebug)
                    Logs.LogWarning("Trying to bound a null reporter !");
                
                return;
            }
            
            _boundReporter = reporter;
            if(Settings.EnableDebug)
                Logs.Log("Successfully bound a new level reporter.");
        }
        
        private async UniTask FinishUnfinishedBusiness()
        {
            // Finish unfinished business
            if (_mapHandle.IsValid() && !_mapHandle.IsDone)
            {
                await _mapHandle;
                
                if (_mapHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    if (Settings.EnableDebug)
                        Logs.LogError($"[LevelSystem]: Tried to load map with unfinished load process: {_mapHandle.OperationException.Message}");
                }
            }
        }

        private async UniTask<List<IResourceLocation>> LoadMapsByLabel(string label)
        {
            var handle = Addressables.LoadResourceLocationsAsync(label, typeof(SceneInstance));
            try
            {
                await handle;

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Logs.Log($"[LevelSystem]: Failed to load the arena maps. Issued: {handle.OperationException.Message}");
                    return null;
                }

                var operations = new List<IResourceLocation>(handle.Result);
                
                if (Settings.EnableDebug)
                    Logs.Log($"Successfully loaded {operations.Count} maps resource locations.");

                return operations;

            } finally
            {
                Addressables.Release(handle);
            }
        }
        
        private LevelReporter _boundReporter;
        private LevelReporter BoundReporter
        {
            get
            {
                if (_boundReporter == null)
                {
                    Logs.LogWarning("No reporter bounded to the Level System !");
                    var reporter = Object.FindFirstObjectByType<LevelReporter>();
                    if (reporter)
                    {
                        Logs.LogWarning("Found a level reporter in the scene ! Fallback to that reporter.");
                        return _boundReporter = reporter;
                    }
                    return null;
                }

                return _boundReporter;
            }
        }

        private Transform _fallbackTransform;
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
        
        public async UniTask OnInitialize()
        {
            // Load the system settings
            _settingsHandle = await SystemManager.Config.LevelSettings.LazyLoadAssetRef();

            _arenaMapLocations = await LoadMapsByLabel(k_arenaMapsLabel);
            _raceMapLocations = await LoadMapsByLabel(k_raceMapsLabel);
        }

        public void Dispose()
        {
            Addressables.Release(_settingsHandle);
            
            _arenaMapLocations.Clear();
        }
        
        public bool IsInitialized { get; set; }
    }
}
