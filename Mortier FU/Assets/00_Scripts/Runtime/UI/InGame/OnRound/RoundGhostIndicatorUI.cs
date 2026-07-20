using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace MortierFu
{
    public class RoundGhostIndicatorUI : MonoBehaviour
    {
        [SerializeField] private List<Image> ghostIndicators;

        [SerializeField] private float screenEdgeOffset = 32f;

        private CameraSystem _cameraSystem;
        private GameModeBase _gameMode;
        private GhostSystem _ghostSystem;


        private void Start()
        {
            _cameraSystem = SystemManager.Instance.Get<CameraSystem>();
            _gameMode = GameService.CurrentGameMode as GameModeBase;
            _ghostSystem = SystemManager.Instance.Get<GhostSystem>();

            if (_cameraSystem == null)
            {
                Logs.LogError("[RoundGhostIndicatorUI]: Missing Camera System or Missing CameraSystem.Controller].");
                return;
            }

            if (_gameMode == null)
            {
                Logs.LogError("[RoundGhostIndicatorUI]: GameMode is null.");
                return;
            }

            if (_ghostSystem == null)
            {
                Logs.LogError("[RoundGhostIndicatorUI]: Ghost System is null.");
                return;
            }
            
            Logs.LogError($"Screen H : {Screen.height} Screen W : {Screen.width}");
        }

        private void Update()
        {
            if (!_cameraSystem.Controller || _ghostSystem == null)
                return;

            foreach (PlayerGhostPawn ghostPawn in _ghostSystem.ActiveGhostsPawns)
            {
                if (!ghostPawn || !ghostPawn.Owner)
                    continue;

                int index = ghostPawn.Owner.PlayerIndex;
                
                if (index < 0 || index >= ghostIndicators.Count || !ghostIndicators[index])
                    continue;

                UpdateGhostUI(ghostPawn.PawnTransform, index);
            }
        }

        private void UpdateGhostUI(Transform ghostT, int index)
        {
            Camera cam = _cameraSystem.Controller.Camera;

            Vector3 flatTargetPos = new Vector3(ghostT.position.x, _cameraSystem.Controller.transform.position.y, ghostT.position.z);

            Vector3 screenPosition = cam.WorldToScreenPoint(flatTargetPos);
            bool behindCamera = screenPosition.z < 0f;

            if (behindCamera)
            {
                screenPosition.x = Screen.width - screenPosition.x;
                screenPosition.y = Screen.height - screenPosition.y;
            }

            bool onScreen = !behindCamera && IsInCameraView(screenPosition);

            Image indicator = ghostIndicators[index];

            if (!onScreen)
            {
                Vector2 clamped = ClampToScreen(screenPosition);
                indicator.rectTransform.position = clamped;
                indicator.gameObject.SetActive(true);
            }
            else
            {
                indicator.gameObject.SetActive(false);
            }
        }

        private bool IsInCameraView(Vector2 screenPosition)
        {
            return screenPosition.x > 0 && screenPosition.x < Screen.width &&
                   screenPosition.y > 0 && screenPosition.y < Screen.height;
        }

        private Vector2 ClampToScreen(Vector2 screenPosition)
        {
            screenPosition.x = Mathf.Clamp(screenPosition.x, screenEdgeOffset, Screen.width - screenEdgeOffset);
            screenPosition.y = Mathf.Clamp(screenPosition.y, screenEdgeOffset, Screen.height - screenEdgeOffset);

            return screenPosition;
        }
    }
}