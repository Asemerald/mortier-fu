using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MortierFu
{
    public class GameEndUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image _winnerImageBackground;

        [Header("Winner UI Input")]
        [SerializeField] private Selectable _firstSelected;

        [Header("Assets")]
        [SerializeField] private Sprite[] _winnerBackgroundSprites;

        [SerializeField] private Button _returnToLobbyButton;
        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _mainMenuButton;

        [SerializeField] private Sprite[] _returnToLobbySprites;
        [SerializeField] private Sprite[] _newGameSprites;
        [SerializeField] private Sprite[] _mainMenuSprites;

        private readonly UnityPlayerUISession _uiSession = new();

        private GameModeBase _gm;
        private GameService _gameService;
        private LobbyService _lobbyService;
        private MultiplayerEventSystem _eventSystem;
        private InputSystemUIInputModule _inputModule;

        private PlayerManager _winnerPlayer;

        private void Awake()
        {
            _gameService = ServiceManager.Instance.Get<GameService>();
            _lobbyService = ServiceManager.Instance.Get<LobbyService>();
            _eventSystem = EventSystem.current as MultiplayerEventSystem;
            _inputModule = _eventSystem ? _eventSystem.currentInputModule as InputSystemUIInputModule : null;

            HideWinnerPresentation();
        }

        private void OnEnable()
        {
            SubscribeGameMode();
            BindButtonEvents();
        }

        private void OnDisable()
        {
            UnsubscribeGameMode();
            EndWinnerUISession();
            UnbindButtonEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeGameMode();
            EndWinnerUISession();
            UnbindButtonEvents();
        }

        private void SubscribeGameMode()
        {
            UnsubscribeGameMode();

            _gm = GameService.CurrentGameMode as GameModeBase;

            if (_gm == null)
                return;

            _gm.OnGameEnded += SetWinner;
        }

        private void UnsubscribeGameMode()
        {
            if (_gm == null)
                return;

            _gm.OnGameEnded -= SetWinner;
            _gm = null;
        }

        private void BindButtonEvents()
        {
            UnbindButtonEvents();

            if (_returnToLobbyButton)
                _returnToLobbyButton.onClick.AddListener(OnClickReturnToLobby);

            if (_newGameButton)
                _newGameButton.onClick.AddListener(OnClickNewGame);

            if (_mainMenuButton)
                _mainMenuButton.onClick.AddListener(OnClickMainMenu);
        }

        private void UnbindButtonEvents()
        {
            if (_returnToLobbyButton)
                _returnToLobbyButton.onClick.RemoveListener(OnClickReturnToLobby);

            if (_newGameButton)
                _newGameButton.onClick.RemoveListener(OnClickNewGame);

            if (_mainMenuButton)
                _mainMenuButton.onClick.RemoveListener(OnClickMainMenu);
        }

        private void OnClickReturnToLobby()
        {
            EndWinnerUISession();
            _gameService?.ReturnToLobby();
        }

        private void OnClickNewGame()
        {
            EndWinnerUISession();
            _gameService?.RestartGame();
        }

        private void OnClickMainMenu()
        {
            EndWinnerUISession();
            _gameService?.ReturnToMainMenu();
        }

        private void SetWinner(int playerIndex)
        {
            if (!IsValidPlayerIndex(playerIndex))
            {
                Logs.LogError($"[GameEndUI] Invalid winner player index: {playerIndex}.");
                return;
            }

            PlayerManager winner = _gm?.GetWinnerPlayer();

            if (!winner)
            {
                Logs.LogError("[GameEndUI] Cannot find winner PlayerManager.");
                return;
            }

            _winnerPlayer = winner;

            ApplyWinnerSprites(playerIndex);
            ShowWinnerPresentation();
            BeginWinnerUISession(winner);
        }

        private void BeginWinnerUISession(PlayerManager winner)
        {
            if (!winner)
                return;

            if (!_eventSystem || !_inputModule)
            {
                Logs.LogError("[GameEndUI] Missing MultiplayerEventSystem or InputSystemUIInputModule reference.");
                return;
            }

            DisableAllPlayersUIInput();

            _uiSession.Begin(winner, _eventSystem, _inputModule, _firstSelected, PlayerControlContext.EndGame);

            _eventSystem.SetSelectedGameObject(null);
            _eventSystem.SetSelectedGameObject(_firstSelected.gameObject);

            Logs.Log($"[GameEndUI] Winner menu controlled by Player {winner.PlayerIndex + 1}.");
        }

        private void EndWinnerUISession()
        {
            _uiSession.End();

            if (_eventSystem)
                _eventSystem.SetSelectedGameObject(null);

            _winnerPlayer = null;
        }

        private void DisableAllPlayersUIInput()
        {
            IReadOnlyList<PlayerManager> players = _lobbyService?.GetPlayers();

            if (players == null)
                return;

            for (int i = 0; i < players.Count; i++)
            {
                PlayerManager player = players[i];

                if (!player)
                    continue;

                player.SetUnityEventSystemUIActive(false);

                if (player != _winnerPlayer)
                    player.SetControlContext(PlayerControlContext.Loading);
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

            return _mainMenuSprites != null && playerIndex < _mainMenuSprites.Length;
        }

        private void ApplyWinnerSprites(int playerIndex)
        {
            if (_winnerImageBackground)
                _winnerImageBackground.sprite = _winnerBackgroundSprites[playerIndex];

            if (_returnToLobbyButton)
                _returnToLobbyButton.image.sprite = _returnToLobbySprites[playerIndex];

            if (_newGameButton)
                _newGameButton.image.sprite = _newGameSprites[playerIndex];

            if (_mainMenuButton)
                _mainMenuButton.image.sprite = _mainMenuSprites[playerIndex];
        }

        private void ShowWinnerPresentation()
        {
            if (_winnerImageBackground)
                _winnerImageBackground.gameObject.SetActive(true);
        }

        private void HideWinnerPresentation()
        {
            if (_winnerImageBackground)
                _winnerImageBackground.gameObject.SetActive(false);
        }
    }
}