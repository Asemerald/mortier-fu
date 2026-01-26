using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MortierFu
{
    public class GameEndUI : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private Image _winnerImageBackground;

        [Header("Assets")] [SerializeField] private Sprite[] _winnerBackgroundSprites;

        [SerializeField] private Button _continueGameButton;
        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _mainMenuButton;

        [SerializeField] private Sprite[] _continueGameSprites;
        [SerializeField] private Sprite[] _newGameSprites;
        [SerializeField] private Sprite[] _mainMenuSprites;
        
        [SerializeField] private GameObject _winPlayer;
        [SerializeField] private SkinnedMeshRenderer[] _playerMeshes;
        [SerializeField] private SkinnedMeshRenderer[] _playerOutlineMeshes;
        [SerializeField] private Material[] _playerOutlineMaterials;
        [SerializeField] private Material[] _playerMaterials;
        
        [SerializeField] private Camera _renderCamera;
        
        private GameModeBase _gm;
        
        private EventSystem _eventSystem;

        private void Awake()
        {
            _gm = GameService.CurrentGameMode as GameModeBase;
            _eventSystem = EventSystem.current;
            
            _renderCamera.gameObject.SetActive(false);
            _winnerImageBackground.gameObject.SetActive(false);
            _winPlayer.gameObject.SetActive(false);
            
            _continueGameButton.onClick.AddListener(OnClickContinueGame);
            _newGameButton.onClick.AddListener(OnClickNewGame);
            _mainMenuButton.onClick.AddListener(OnClickMainMenu);
        }

        private void OnEnable()
        {
            if (_gm != null)
            {
                _gm.OnGameEnded += SetWinner;
            }
        }

        private void OnDisable()
        {
            _gm.OnGameEnded -= SetWinner;
        }

        private void OnClickContinueGame()
        {
          //  int scoreToWin = MenuManager.Instance.LobbyPanel.SelectedMaxScore;
            
         //   _gm.SetScoreToWin(scoreToWin * 2);
        }

        private void OnClickNewGame()
        {
            ServiceManager.Instance.Get<GameService>().RestartGame();
        }
        
        private void OnClickMainMenu()
        {
          _gm.ReturnToMainMenu();
        }

        private void SetWinner(int playerIndex)
        {
            if (playerIndex >= 0 && playerIndex < _winnerBackgroundSprites.Length)
            {
                _winnerImageBackground.sprite = _winnerBackgroundSprites[playerIndex];
                _continueGameButton.image.sprite = _continueGameSprites[playerIndex];
                _newGameButton.image.sprite = _newGameSprites[playerIndex];
                _mainMenuButton.image.sprite = _mainMenuSprites[playerIndex];

                foreach (var mesh in _playerMeshes)
                {
                    mesh.material = _playerMaterials[playerIndex];
                }
                
                foreach (var outlineMesh in _playerOutlineMeshes)
                {
                    outlineMesh.material = _playerOutlineMaterials[playerIndex];
                }
                
                _winnerImageBackground.gameObject.SetActive(true);
                _renderCamera.gameObject.SetActive(true);
                _winPlayer.gameObject.SetActive(true);
                
                _eventSystem.SetSelectedGameObject(_mainMenuButton.gameObject);
            }
            else
            {
                Debug.LogError("Invalid PlayerIndex");
            }
        }
    }
}