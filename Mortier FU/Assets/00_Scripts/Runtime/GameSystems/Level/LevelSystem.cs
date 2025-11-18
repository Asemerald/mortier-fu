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
        private SO_LevelSettings _settings;
        
        private List<IResourceLocation> _mapLocations;
        
        // Used to track and unload the current loaded map
        private AsyncOperationHandle<SceneInstance> _mapHandle;

        private const string k_arenaMapsLabel = "ArenaMaps";

        public async UniTask LoadAugmentMap()
        {
            await FinishUnfinishedBusiness();
            await UnloadCurrentMap();

            object augmentMapKey = _settings.AugmentMapScene.RuntimeKey;
            _mapHandle = Addressables.LoadSceneAsync(augmentMapKey, LoadSceneMode.Additive, SceneReleaseMode.OnlyReleaseSceneOnHandleRelease);
            
            await _mapHandle;
        }

        public async UniTask LoadGameplayMap()
        {
            await FinishUnfinishedBusiness();
            await UnloadCurrentMap();
            
            #if UNITY_EDITOR
            string sceneKey = EditorPrefs.GetString("OverrideMapAddress", "");
            if (!string.IsNullOrEmpty(sceneKey)) {
                var locations = await Addressables.LoadResourceLocationsAsync(sceneKey);
                if (locations.Count > 0) {
                    _mapHandle = Addressables.LoadSceneAsync(sceneKey, LoadSceneMode.Additive, SceneReleaseMode.ReleaseSceneWhenSceneUnloaded);
                    await _mapHandle;
                
                    if(_settings.EnableDebug)
                        Logs.Log($"[LevelSystem]: Enforce the use of the debug scene: {sceneKey}");
                
                    return;
                }
                else 
                {
                    if(_settings.EnableDebug)
                        Logs.LogWarning($"[LevelSystem]: Debug scene key not found in Addressables: {sceneKey}");
                }

            }
            #endif
            
            var map = _mapLocations.RandomElement();
            _mapHandle = Addressables.LoadSceneAsync(map, LoadSceneMode.Additive, SceneReleaseMode.ReleaseSceneWhenSceneUnloaded);

            await _mapHandle;
            
            if(_settings.EnableDebug)
                Logs.Log($"[LevelSystem]: Random map selected: {_mapHandle.Result.Scene.name} !");
        }
        
        private async UniTask UnloadCurrentMap()
        {
            if (!_mapHandle.IsValid()) return;

            await Addressables.UnloadSceneAsync(_mapHandle);
            _mapHandle = default;
        }

        public Transform GetSpawnPoint(int index)
        {
            if (BoundReporter == null)
                return FallbackTransform;

            if (index < 0 || index >= BoundReporter.SpawnPoints.Length)
            {
                if(_settings.EnableDebug) 
                    Logs.LogWarning("[LevelSystem]: Trying to get a spawn point which is out of range of the provided list of spawn points by the level reporter !");
                return FallbackTransform;
            }
            
            return BoundReporter.SpawnPoints[index];
        }

        public void PopulateAugmentPoints(Vector3[] outPoints)
        {
            if (BoundReporter == null)
            {
                for (int i = 0; i < outPoints.Length; i++)
                {
                    outPoints[i] = Vector3.zero;
                    return;
                }
            }

            var pivot = BoundReporter.AugmentPivot ?? FallbackTransform;
            for (int i = 0; i < outPoints.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / outPoints.Length;
                Vector3 point = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * BoundReporter.Radius;
                outPoints[i] = pivot.position + point;
            }
        }
        
        public Transform GetAugmentPivot() => BoundReporter != null ? BoundReporter.AugmentPivot ?? FallbackTransform : FallbackTransform;
        
        public void BindReporter(LevelReporter reporter)
        {
            if (reporter == null)
            {
                if (_settings.EnableDebug)
                    Logs.LogWarning("Trying to bound a null reporter !");
                
                return;
            }
            
            _boundReporter = reporter;
            if(_settings.EnableDebug)
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
                    if (_settings.EnableDebug)
                        Logs.LogError($"[LevelSystem]: Tried to load map with unfinished load process: {_mapHandle.OperationException.Message}");
                }
            }
        }
        
        private async UniTask LoadAllMaps()
        {
            var handle = Addressables.LoadResourceLocationsAsync(k_arenaMapsLabel, typeof(SceneInstance));
            try
            {
                await handle;

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Logs.Log($"[LevelSystem]: Failed to load the maps. Issued: {handle.OperationException.Message}");
                    return;
                }

                _mapLocations = new List<IResourceLocation>(handle.Result);
                
                if (_settings.EnableDebug)
                    Logs.Log($"Successfully loaded {_mapLocations.Count} maps resource locations.");
                
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
            var settingsRef = SystemManager.Config.LevelSettings;
            _settings = await AddressablesHelpers.LazyLoadAsset(settingsRef);
            if (_settings == null) return;

            await LoadAllMaps();
        }

        public void Dispose()
        {
            _mapLocations.Clear();
        }
        
        public bool IsInitialized { get; set; }
    }
}
