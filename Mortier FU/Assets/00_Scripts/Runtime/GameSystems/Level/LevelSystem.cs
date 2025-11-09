using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MortierFu
{
    public class LevelSystem : IGameSystem
    {
        private AsyncOperationHandle<SO_LevelSettings> _settingsHandle;

        public SO_LevelSettings Settings => _settingsHandle.Result;

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

        public Transform GetSpawnPoint(int index)
        {
            if (BoundReporter == null)
                return FallbackTransform;

            if (index < 0 || index >= BoundReporter.SpawnPoints.Length)
            {
                if(Settings.EnableDebug) 
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
                if(Settings.EnableDebug) 
                    Logs.LogWarning("[LevelSystem]: Trying to get an augment point which is out of range of the provided list of augment points by the level reporter !");
                return FallbackTransform;
            }
            
            return BoundReporter.SpawnPoints[index];
        }
        
        public async UniTask OnInitialize()
        {
            // Load the system settings
            _settingsHandle = SystemManager.Config.LevelSettings.LoadAssetAsync();
            await _settingsHandle;

            if (_settingsHandle.Status != AsyncOperationStatus.Succeeded)
            {
                if (Settings.EnableDebug)
                {
                    Logs.LogError("[LevelSystem]: Failed while loading settings using Addressables. Error: " + _settingsHandle.OperationException.Message);
                }
                return;
            }
        }
        
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
        
        public void Dispose()
        {
            Addressables.Release(_settingsHandle);
        }
        
        public bool IsInitialized { get; set; }
    }
}
