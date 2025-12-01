using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class CameraSystem : IGameSystem
    {
        private GameObject _cameraPrefab;

        private CameraController _cameraController;
        private CameraShakeController _shakeController;

        public CameraController Controller { get; private set; }
        public CameraShakeController ShakeController { get; private set; }
        public SO_CameraSettings Settings { get; private set; }

        private Transform _cameraRoot;

        private void SpawnCamera()
        {
            if (_cameraController != null)
                return;

            if (_cameraPrefab == null)
            {
                Logs.LogError("[CameraSystem] Cannot spawn camera, prefab is null. Did OnInitialize succeed?");
                return;
            }

            var camGo = Object.Instantiate(_cameraPrefab, _cameraRoot);
            camGo.name = "GameCamera";

            if (camGo.TryGetComponent(out CameraController cameraController))
            {
                _cameraController = cameraController;
                Controller = _cameraController;
            }

            if (!camGo.TryGetComponent(out CameraShakeController shakeController)) return;

            _shakeController = shakeController;
            ShakeController = _shakeController;
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

            _cameraPrefab = await AddressablesUtils.LazyLoadAsset(Settings.CameraPrefab);
            if (_cameraPrefab == null)
            {
                Logs.LogError("[CameraSystem] Camera prefab not found in settings.");
                return;
            }

            _cameraRoot = new GameObject("CameraRoot").transform;

            SpawnCamera();

            await UniTask.CompletedTask;
        }

        public bool IsInitialized { get; set; }

        public void Dispose()
        {
            if (_cameraController != null)
            {
                Object.Destroy(_cameraController.gameObject);
                _cameraController = null;
                _shakeController = null;
            }

            if (_cameraRoot != null)
            {
                Object.Destroy(_cameraRoot.gameObject);
                _cameraRoot = null;
            }

            Logs.Log("[CameraSystem] Disposed.");
        }
    }
}