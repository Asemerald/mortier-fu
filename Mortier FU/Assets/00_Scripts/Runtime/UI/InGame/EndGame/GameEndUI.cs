using MortierFu.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MortierFu
{
    public class GameEndUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image _winnerImageBackground;

        [Header("Assets")]
        [SerializeField] private Sprite[] _winnerBackgroundSprites;

        [SerializeField] private Button _returnToLobbyButton;

        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _mainMenuButton;

        [SerializeField] private Sprite[] _returnToLobbySprites;

        [SerializeField] private Sprite[] _newGameSprites;
        [SerializeField] private Sprite[] _mainMenuSprites;

        [Header("Winner Preview")]
        [SerializeField] private GameObject _winPlayer;
        [SerializeField] private SkinnedMeshRenderer[] _playerMeshes;
        [SerializeField] private SkinnedMeshRenderer[] _playerOutlineMeshes;
        [SerializeField] private Material[] _playerOutlineMaterials;
        [SerializeField] private Material[] _playerMaterials;

        [SerializeField] private Camera _renderCamera;

        private GameModeBase _gm;
        private GameService _gameService;
        private EventSystem _eventSystem;

        private void Awake()
        {
            _gm = GameService.CurrentGameMode as GameModeBase;
            _gameService = ServiceManager.Instance.Get<GameService>();
            _eventSystem = EventSystem.current;

            HideWinnerPresentation();

            if (_returnToLobbyButton != null)
                _returnToLobbyButton.onClick.AddListener(OnClickReturnToLobby);

            if (_newGameButton != null)
                _newGameButton.onClick.AddListener(OnClickNewGame);

            if (_mainMenuButton != null)
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
            if (_gm != null)
            {
                _gm.OnGameEnded -= SetWinner;
            }
        }

        private void OnDestroy()
        {
            if (_returnToLobbyButton != null)
                _returnToLobbyButton.onClick.RemoveListener(OnClickReturnToLobby);

            if (_newGameButton != null)
                _newGameButton.onClick.RemoveListener(OnClickNewGame);

            if (_mainMenuButton != null)
                _mainMenuButton.onClick.RemoveListener(OnClickMainMenu);
        }

        private void OnClickReturnToLobby()
        {
            _gameService?.ReturnToLobby();
        }

        private void OnClickNewGame()
        {
            _gameService?.RestartGame();
        }

        private void OnClickMainMenu()
        {
            _gameService?.ReturnToMainMenu();
        }

        private void SetWinner(int playerIndex)
        {
            if (!IsValidPlayerIndex(playerIndex))
            {
                Logs.LogError($"[GameEndUI] Invalid winner player index: {playerIndex}.");
                return;
            }

            ApplyWinnerSprites(playerIndex);
            ApplyWinnerMaterials(playerIndex);
            ShowWinnerPresentation();

            if (_eventSystem != null && _mainMenuButton != null)
            {
                _eventSystem.SetSelectedGameObject(_mainMenuButton.gameObject);
            }
        }

        private bool IsValidPlayerIndex(int playerIndex)
        {
            if (playerIndex < 0)
                return false;

            if (_winnerBackgroundSprites == null || playerIndex >= _winnerBackgroundSprites.Length)
                return false;

            if (_returnToLobbySprites == null || playerIndex >= _returnToLobbySprites.Length)
                return false;

            if (_newGameSprites == null || playerIndex >= _newGameSprites.Length)
                return false;

            if (_mainMenuSprites == null || playerIndex >= _mainMenuSprites.Length)
                return false;

            if (_playerMaterials == null || playerIndex >= _playerMaterials.Length)
                return false;

            if (_playerOutlineMaterials == null || playerIndex >= _playerOutlineMaterials.Length)
                return false;

            return true;
        }

        private void ApplyWinnerSprites(int playerIndex)
        {
            if (_winnerImageBackground != null)
            {
                _winnerImageBackground.sprite = _winnerBackgroundSprites[playerIndex];
            }

            if (_returnToLobbyButton != null)
            {
                _returnToLobbyButton.image.sprite = _returnToLobbySprites[playerIndex];
            }

            if (_newGameButton != null)
            {
                _newGameButton.image.sprite = _newGameSprites[playerIndex];
            }

            if (_mainMenuButton != null)
            {
                _mainMenuButton.image.sprite = _mainMenuSprites[playerIndex];
            }
        }

        private void ApplyWinnerMaterials(int playerIndex)
        {
            if (_playerMeshes != null)
            {
                foreach (var mesh in _playerMeshes)
                {
                    if (mesh != null)
                    {
                        mesh.material = _playerMaterials[playerIndex];
                    }
                }
            }

            if (_playerOutlineMeshes != null)
            {
                foreach (var outlineMesh in _playerOutlineMeshes)
                {
                    if (outlineMesh != null)
                    {
                        outlineMesh.material = _playerOutlineMaterials[playerIndex];
                    }
                }
            }
        }

        private void ShowWinnerPresentation()
        {
            if (_winnerImageBackground != null)
                _winnerImageBackground.gameObject.SetActive(true);

            if (_renderCamera != null)
                _renderCamera.gameObject.SetActive(true);

            if (_winPlayer != null)
                _winPlayer.SetActive(true);
        }

        private void HideWinnerPresentation()
        {
            if (_renderCamera != null)
                _renderCamera.gameObject.SetActive(false);

            if (_winnerImageBackground != null)
                _winnerImageBackground.gameObject.SetActive(false);

            if (_winPlayer != null)
                _winPlayer.SetActive(false);
        }
    }
}