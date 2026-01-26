using System;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine.UI;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MortierFu
{
    public class LobbyPanel : UIPanel
    {
        [Header("Dependencies")] [SerializeField]
        private LobbyMenu3D lobbyMenu3D;

        [Header("Buttons References")] [SerializeField]
        private Button startGameButton;

        [SerializeField] private ScoreToWinStep[] _steps;
        [SerializeField] private Button _topUpdateMaxScoreBtn;
        [SerializeField] private Button _bottomUpdateMaxScoreBtn;

        [SerializeField] private Image _maxScoreImg;

        private int _currentMaxScoreIndex = 3;

        void Awake()
        {
            // Resolve dependencies
            if (lobbyMenu3D == null)
                Logs.LogError("[LobbyPanel]: LobbyMenu3D reference is missing.", this);
            if (startGameButton == null)
                Logs.LogError("[LobbyPanel]: StartGameButton reference is missing.", this);
        }

        private void Start()
        {
            UpdateMaxScore();
            Hide();
#if UNITY_EDITOR
            if (EditorPrefs.GetBool("SkipMenuEnabled", false))
            {
                MenuManager.Instance.StartGame().Forget();
            }
#endif
        }

        private void OnEnable()
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
            _topUpdateMaxScoreBtn.onClick.AddListener(OnTopUpdateMaxScoreClicked);
            _bottomUpdateMaxScoreBtn.onClick.AddListener(OnBottomUpdateMaxScoreClicked);
            PlayerInputBridge.Instance.CanJoin(true);
        }

        private void OnDisable()
        {
            startGameButton.onClick.RemoveListener(OnStartGameClicked);
            _topUpdateMaxScoreBtn.onClick.RemoveListener(OnTopUpdateMaxScoreClicked);
            _bottomUpdateMaxScoreBtn.onClick.RemoveListener(OnBottomUpdateMaxScoreClicked);
        }
        
        public int SelectedMaxScore => _steps[_currentMaxScoreIndex].ScoreToWin;

        private void OnStartGameClicked()
        {
            MenuManager.Instance.StartGame().Forget();
        }

        private void OnTopUpdateMaxScoreClicked()
        {
            _currentMaxScoreIndex++;

            if (_currentMaxScoreIndex >= _steps.Length)
                _currentMaxScoreIndex = 0;
            
            UpdateMaxScore();
        }

        private void OnBottomUpdateMaxScoreClicked()
        {
            _currentMaxScoreIndex--;

            if (_currentMaxScoreIndex < 0)
                _currentMaxScoreIndex = _steps.Length - 1;
            
            UpdateMaxScore();
        }

        private void UpdateMaxScore()
        {
            if (_currentMaxScoreIndex < 0 || _currentMaxScoreIndex >= _steps.Length)
                return;

            var step = _steps[_currentMaxScoreIndex];
            _maxScoreImg.sprite = step.StepSprite;
            _maxScoreImg.SetNativeSize();
        }

        [Serializable]
        private struct ScoreToWinStep
        {
            public int ScoreToWin;
            public Sprite StepSprite;
        }
    }
}