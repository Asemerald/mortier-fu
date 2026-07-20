using System;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.Serialization;

namespace MortierFu
{
    public class RoundGhostIndicatorUI : MonoBehaviour
    {
        [SerializeField] private RectTransform canvasGhostIndicatorRecT;
        
        private CameraSystem _cameraSystem;
        private GameModeBase _gameMode;
        
        private Transform _cameraTransformCache;

        private void OnEnable()
        {
            _cameraSystem = ServiceManager.Instance.Get<CameraSystem>();
            _gameMode = GameService.CurrentGameMode as GameModeBase;
            
            if (_cameraSystem == null || !_cameraSystem.Controller)
            {
                Logs.LogError("[RoundGhostIndicatorUI]: Missing Camera System or Missing CameraSystem.Controller].");
                return;
            }

            if (_gameMode == null)
            {
                Logs.LogError("[RoundGhostIndicatorUI]: GameMode is null.");
                return;
            }
            
            _cameraTransformCache = _cameraSystem.Controller.transform;
        }

        /*private Vector2 GetGhostInUiSpace(Transform target)
        {
            Vector3 forwardCamera = _cameraTransformCache.forward;
            forwardCamera.y = 0;
            forwardCamera.Normalize();
            
            Vector3 rightCamera = _cameraTransformCache.right;
            rightCamera.y = 0;
            rightCamera.Normalize();
            
            Vector3 direction = target.position - _cameraTransformCache.position;
            
            float forwardDot = Vector3.Dot(forwardCamera, direction);
            float rightDot = Vector3.Dot(rightCamera, direction);
            
            return new Vector2(rightDot, forwardDot).normalized;
        }*/

        private void Test(Transform target, RectTransform indicator)
        {
            Vector3 screenPosition = _cameraSystem.Controller.Camera.WorldToScreenPoint(target.position);
            
            bool isTargetOffScreen = screenPosition.x <= 0 || screenPosition.x >= 1 || screenPosition.y <= 0 || screenPosition.y >= 1;
        }
    }
}