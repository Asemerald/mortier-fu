using System;
using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace MortierFu
{
    public class RoundGhostIndicatorUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private List<Sprite> ghostsIcons = new();
        [SerializeField] private Image ghostIconPrefab;

        [Space(15)]
        
        [Header("Settings")]
        [SerializeField] private float screenEdgeOffset = 16f;
        
        private CameraSystem _cameraSystem;
        private GameModeBase _gameMode;
        private GhostSystem _ghostSystem;
        
        private readonly List<Image> _ghostIndicators = new();
        
        private EventBinding<TriggerEndRound> _endRoundBinding;
        
        private bool _isInitialized;
        private bool _enable = false;
        

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

            _gameMode.OnRoundEnded += CleanGhostUiAfterRound;
            _gameMode.OnRoundGameplayStarted += Initialize;

            if (_ghostSystem == null)
            {
                Logs.LogError("[RoundGhostIndicatorUI]: Ghost System is null.");
                return;
            }
            
            _endRoundBinding = new EventBinding<TriggerEndRound>(CleanGhostUiAfterRound);
            EventBus<TriggerEndRound>.Register(_endRoundBinding);
        }

        private void OnDestroy()
        {
            _gameMode.OnRoundEnded -= CleanGhostUiAfterRound;
            _gameMode.OnRoundGameplayStarted -= Initialize;
            
            EventBus<TriggerEndRound>.Deregister(_endRoundBinding);
            _endRoundBinding = null;
        }

        private void Update() => HandleGhostIndicatorUI();

        private void Initialize(RoundInfo info)
        {
            _enable = true;
            
            if(_gameMode == null) return;
            
            if (_isInitialized) return;

            int count = _gameMode.MaxPlayerCount; //couldn't get the _gameMode.Teams.Count because not init 

            for (int i = 0; i < count; i++)
            {
                Image ghostIndicator = Instantiate(ghostIconPrefab, transform);

                if (ghostsIcons != null && ghostsIcons[i])
                    ghostIndicator.sprite = ghostsIcons[i];
                
                ghostIndicator.gameObject.SetActive(false);
                
                _ghostIndicators.Add(ghostIndicator);
            }

            _isInitialized = true;
        }

        private void HandleGhostIndicatorUI()
        {
            if(!_isInitialized || !_enable) return;
            
            if (!_cameraSystem.Controller || _ghostSystem == null) return;
            
            foreach (PlayerGhostPawn ghost in _ghostSystem.ActiveGhostsPawns)
            {
                if (!ghost || !ghost.Owner) continue;
                
                int index = ghost.Owner.PlayerIndex;
                
                if(!IsIndexValid(index)) continue;

                UpdateGhostUI(ghost.PawnTransform, index);
            }
        }

        private void UpdateGhostUI(Transform ghostT, int index)
        {
            Camera cam = _cameraSystem.Controller.Camera;

            Vector3 screenPosition = cam.WorldToScreenPoint(ghostT.position);
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            Image indicator = _ghostIndicators[index];

            if (!IsGhostOnScreen(screenPosition))
            {
                Vector2 fromCenter = (Vector2)screenPosition - screenCenter;
                Vector2 clampedPosition = GetScreenEdgePosition(fromCenter, screenCenter, screenEdgeOffset);

                indicator.rectTransform.position = clampedPosition;
                indicator.gameObject.SetActive(true);
            }
            else
            {
                indicator.gameObject.SetActive(false);
            }
        }

        private Vector2 GetScreenEdgePosition(Vector2 fromCenter, Vector2 screenCenter, float offset)
        {
            float maxX = screenCenter.x - offset;
            float maxY = screenCenter.y - offset;

            float divX = Mathf.Abs(fromCenter.x) < 0.0001f ? 0.0001f : Mathf.Abs(fromCenter.x);
            float divY = Mathf.Abs(fromCenter.y) < 0.0001f ? 0.0001f : Mathf.Abs(fromCenter.y);

            float scaleX = maxX / divX;
            float scaleY = maxY / divY;

            float scale = Mathf.Min(scaleX, scaleY);

            return screenCenter + (fromCenter * scale);
        }

        private bool IsIndexValid(int index)
        {
            return index >= 0 && index < ghostsIcons.Count && ghostsIcons[index];
        }

        private bool IsGhostOnScreen(Vector2 screenPosition)
        {
            return screenPosition.x >= screenEdgeOffset && 
                            screenPosition.x <= Screen.width - screenEdgeOffset &&
                            screenPosition.y >= screenEdgeOffset && 
                            screenPosition.y <= Screen.height - screenEdgeOffset;
        }

        private void CleanGhostUiAfterRound(RoundInfo roundInfo)
        {
            _enable = false;
            
            foreach (Image ghostIndicator in _ghostIndicators)
                ghostIndicator.gameObject.SetActive(false);
        }

        private void CleanGhostUiAfterRound() //need polymorphism for RaceStart not include RoundInfo
        {
            _enable = false;
            
            foreach (Image ghostIndicator in _ghostIndicators)
                ghostIndicator.gameObject.SetActive(false);
        }
    }
}