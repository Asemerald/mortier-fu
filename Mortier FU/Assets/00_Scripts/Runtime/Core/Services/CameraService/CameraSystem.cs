using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class CameraSystem : IGameSystem
    {
        public CameraController Controller { get; private set; }
        public CameraShakeController ShakeController { get; private set; }
        public SO_CameraSettings Settings { get; private set; }

        private Transform _cameraRoot;

        private void SpawnCamera(GameObject cameraPrefab)
        {
            var camGo = Object.Instantiate(cameraPrefab, _cameraRoot);
            camGo.name = "GameCamera";

            Controller = camGo.GetComponent<CameraController>();
            ShakeController = camGo.GetComponent<CameraShakeController>();
        }

        public async UniTask OnInitialize()
        {
            var settingsRef = SystemManager.Config.CameraSettings;
            Settings = await AddressablesUtils.LazyLoadAsset(settingsRef);
            if (Settings == null)
            {
                Logs.LogError("[CameraSystem] CameraSettings not found.");
                return;
            }
            
            _cameraRoot = new GameObject("CameraRoot").transform;

            var cameraPrefab = await AddressablesUtils.LazyLoadAsset(Settings.CameraPrefab);
            if (cameraPrefab == null)
            {
                Logs.LogError("[CameraSystem] Camera prefab not found.");
                return;
            }
            
            SpawnCamera(cameraPrefab);

            await UniTask.CompletedTask;
        }

        public bool IsInitialized { get; set; }

        public void Dispose()
        {

            if (_cameraRoot != null)
            {
                Object.Destroy(_cameraRoot.gameObject);
                _cameraRoot = null;
            }

            Logs.Log("[CameraSystem] Disposed.");
        }
    }
}