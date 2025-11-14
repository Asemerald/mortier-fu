using UnityEngine;
using MortierFu.Shared;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace MortierFu
{
    public class LevelSystem : IGameSystem
    {
        private SO_LevelSettings _settings;
        
        private List<IResourceLocation> _mapLocations;
        
        // Used to track and unload the current loaded map
        private AsyncOperationHandle<SceneInstance> _mapHandle;

        private const string k_gameplayMapLabel = "GameplayMap";

        public async UniTask LoadAugmentMap()
        {
            await FinishUnfinishedBusiness();
            await UnloadCurrentMap(true);

            object augmentMapKey = _settings.AugmentMapScene.RuntimeKey;
            _mapHandle = Addressables.LoadSceneAsync(augmentMapKey, LoadSceneMode.Additive);
            
            await _mapHandle;
        }

        public async UniTask LoadGameplayMap()
        {
            await FinishUnfinishedBusiness();
            await UnloadCurrentMap(false);
            
            #if UNITY_EDITOR
            string sceneKey = PlayerPrefs.GetString("DebugSceneName", "");
            if (!string.IsNullOrEmpty(sceneKey))
            {
                _mapHandle = Addressables.LoadSceneAsync(sceneKey, LoadSceneMode.Additive, SceneReleaseMode.OnlyReleaseSceneOnHandleRelease);
                
                if(_settings.EnableDebug)
                    Logs.Log($"[LevelSystem]: Enforce the use of the following scene: {sceneKey}");
                
                await _mapHandle;
                return;
            }
            #endif
            
            var map = _mapLocations.RandomElement();
            _mapHandle = Addressables.LoadSceneAsync(map, LoadSceneMode.Additive, SceneReleaseMode.OnlyReleaseSceneOnHandleRelease);

            await _mapHandle;
        }
        
        private async UniTask UnloadCurrentMap(bool releaseHandle)
        {
            if (!_mapHandle.IsValid()) return;

            await Addressables.UnloadSceneAsync(_mapHandle, autoReleaseHandle: releaseHandle);
            
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

        public Transform GetAugmentLocation(int index)
        {
            if (BoundReporter == null)
                return FallbackTransform;

            if (index < 0 || index >= BoundReporter.AugmentPoints.Length)
            {
                if(_settings.EnableDebug) 
                    Logs.LogWarning("[LevelSystem]: Trying to get an augment point which is out of range of the provided list of augment points by the level reporter !");
                return FallbackTransform;
            }
            
            return BoundReporter.AugmentPoints[index];
        }
        
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
            var handle = Addressables.LoadResourceLocationsAsync(k_gameplayMapLabel, typeof(SceneInstance));
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
