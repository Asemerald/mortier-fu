using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MortierFu
{
    public class CameraSystem : IGameSystem
    {
        public CameraController Controller { get; private set; }
        public CameraShakeController ShakeController { get; private set; }

        // Addressables settings
        private AsyncOperationHandle<SO_CameraSettings> _settingsHandle;
        public SO_CameraSettings Settings => _settingsHandle.Result;
        
        private GameObject _cameraGo;

        private async UniTask InstantiateCamera()
        {
            _cameraGo = await Settings.CameraPrefab.InstantiateAsync();
            _cameraGo.name = "GameCamera";
            
            Controller = _cameraGo.GetComponent<CameraController>();
            ShakeController = _cameraGo.GetComponent<CameraShakeController>();
        }

        public async UniTask OnInitialize()
        {
            var settingsRef = SystemManager.Config.CameraSettings;
            _settingsHandle = await settingsRef.LazyLoadAssetRef();
            
            await InstantiateCamera();

            await UniTask.CompletedTask;
        }

        public bool IsInitialized { get; set; }

        public void Dispose()
        {
            Addressables.ReleaseInstance(_cameraGo);
            Addressables.Release(_settingsHandle);
        }
    }
}