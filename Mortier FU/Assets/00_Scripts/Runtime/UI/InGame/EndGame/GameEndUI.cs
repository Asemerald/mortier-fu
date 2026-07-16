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

        private void ShowWinnerPresentation()
        {
            if (_winnerImageBackground != null)
                _winnerImageBackground.gameObject.SetActive(true);
        }

        private void HideWinnerPresentation()
        {
            if (_winnerImageBackground != null)
                _winnerImageBackground.gameObject.SetActive(false);
        }
    }
}