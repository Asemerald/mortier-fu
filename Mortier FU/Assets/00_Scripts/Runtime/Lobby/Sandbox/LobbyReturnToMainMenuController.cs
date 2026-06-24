using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MortierFu
{
    public sealed class LobbyReturnToMainMenuController : MonoBehaviour, IPlayerUIInputHandler
    {
        [Header("References")]
        [SerializeField] private LobbySandboxController _sandboxController;
        [SerializeField] private LobbySandboxStateController _stateController;
        [SerializeField] private LobbyStartReadyController _readyController;

        [Header("Confirmation UI")]
        [SerializeField] private GameObject _confirmPanel;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _defaultSelectedButton;

        [Header("Options")]
        [SerializeField] private bool _despawnLobbyCharacters = true;
        [SerializeField] private bool _disableJoining = true;
        [SerializeField] private bool _onlyPlayerOneCanUse = true;

        private PlayerManager _activePlayer;
        private PlayerControlContext _previousActivePlayerContext;

        private bool _isConfirmationOpen;
        private bool _isReturning;

        private PlayerUIInputService UIInputService =>
            ServiceManager.Instance?.Get<PlayerUIInputService>();

        private void Awake()
        {
            ValidateReferences();

            if (_confirmPanel)
                _confirmPanel.SetActive(false);

            if (_confirmButton)
                _confirmButton.onClick.AddListener(ConfirmReturnToMainMenu);

            if (_cancelButton)
                _cancelButton.onClick.AddListener(CancelReturnToMainMenu);
        }

        private void OnDestroy()
        {
            UIInputService?.RemoveFromAll(this);

            if (_confirmButton)
                _confirmButton.onClick.RemoveListener(ConfirmReturnToMainMenu);

            if (_cancelButton)
                _cancelButton.onClick.RemoveListener(CancelReturnToMainMenu);

            ResumeLobby();
        }

        private void ValidateReferences()
        {
            if (!_sandboxController)
                Logs.LogWarning("[LobbyReturnToMainMenuController] SandboxController reference is missing.");

            if (!_stateController)
                Logs.LogWarning("[LobbyReturnToMainMenuController] SandboxStateController reference is missing.");

            if (!_readyController)
                Logs.LogWarning("[LobbyReturnToMainMenuController] ReadyController reference is missing.");

            if (!_confirmPanel)
                Logs.LogWarning("[LobbyReturnToMainMenuController] ConfirmPanel reference is missing.");

            if (!_confirmButton)
                Logs.LogWarning("[LobbyReturnToMainMenuController] ConfirmButton reference is missing.");

            if (!_cancelButton)
                Logs.LogWarning("[LobbyReturnToMainMenuController] CancelButton reference is missing.");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isConfirmationOpen || _isReturning)
                return;

            var character = ResolvePlayerCharacter(other);

            if (!character || !character.Owner)
                return;

            TryOpenConfirmation(character.Owner);
        }

        private PlayerCharacter ResolvePlayerCharacter(Collider other)
        {
            if (!other)
                return null;

            if (other.TryGetComponent(out PlayerCharacter directCharacter))
                return directCharacter;

            var attachedRigidbody = other.attachedRigidbody;

            if (attachedRigidbody &&
                attachedRigidbody.TryGetComponent(out PlayerCharacter rigidbodyCharacter))
            {
                return rigidbodyCharacter;
            }

            return other.GetComponentInParent<PlayerCharacter>();
        }

        private void TryOpenConfirmation(PlayerManager player)
        {
            if (!player)
                return;

            if (_onlyPlayerOneCanUse && player.PlayerIndex != 0)
                return;

            if (_stateController && !_stateController.CanUseStartTarget())
                return;

            if (!IsPlayerInSandbox(player))
                return;

            OpenConfirmation(player);
        }

        private bool IsPlayerInSandbox(PlayerManager player)
        {
            if (!player)
                return false;

            if (!_sandboxController)
                return false;

            var players = _sandboxController.GetSpawnedPlayers();

            for (int i = 0; i < players.Count; i++)
            {
                if (ReferenceEquals(players[i], player))
                    return true;
            }

            return false;
        }

        private void OpenConfirmation(PlayerManager player)
        {
            _activePlayer = player;
            _previousActivePlayerContext = player.ControlContext;
            _isConfirmationOpen = true;

            PauseLobby();

            if (_sandboxController)
                _sandboxController.LockAllPlayers();

            player.SetControlContext(PlayerControlContext.LobbyReturnConfirmationOwner);

            UIInputService?.Push(player, this);

            if (_confirmPanel)
                _confirmPanel.SetActive(true);

            SelectDefaultButton();

            Logs.Log($"[LobbyReturnToMainMenuController] Confirmation opened by Player {player.PlayerIndex + 1}.");
        }

        private void SelectDefaultButton()
        {
            var buttonToSelect = _defaultSelectedButton;

            if (!buttonToSelect)
                buttonToSelect = _confirmButton;

            if (!buttonToSelect)
                return;

            EventSystem.current?.SetSelectedGameObject(buttonToSelect.gameObject);
        }
        
        private void ConfirmReturnToMainMenu()
        {
            if (_isReturning)
                return;

            ReturnToMainMenuAsync().Forget();
        }

        private void CancelReturnToMainMenu()
        {
            if (!_isConfirmationOpen || _isReturning)
                return;

            Logs.Log("[LobbyReturnToMainMenuController] Return to main menu canceled.");

            CloseConfirmation();
            ResumeLobby();
        }

        private void CloseConfirmation()
        {
            if (_activePlayer)
                UIInputService?.Remove(_activePlayer, this);
            else
                UIInputService?.RemoveFromAll(this);

            if (_confirmPanel)
                _confirmPanel.SetActive(false);

            if (_sandboxController)
                _sandboxController.UnlockAllPlayers();

            RestoreActivePlayerContext();

            _activePlayer = null;
            _isConfirmationOpen = false;
        }

        private void RestoreActivePlayerContext()
        {
            if (!_activePlayer)
                return;

            if (_sandboxController)
            {
                _sandboxController.ApplyCurrentContextToPlayer(_activePlayer);
                return;
            }

            _activePlayer.SetControlContext(_previousActivePlayerContext);
        }

        private async UniTaskVoid ReturnToMainMenuAsync()
        {
            _isReturning = true;

            Logs.Log("[LobbyReturnToMainMenuController] Returning to main menu from lobby.");

            if (_activePlayer)
                UIInputService?.Remove(_activePlayer, this);
            else
                UIInputService?.RemoveFromAll(this);

            if (_confirmPanel)
                _confirmPanel.SetActive(false);

            ResumeLobby();

            LockLobby();
            ResetReadyState();
            DisableJoining();
            ClearActiveBombshells();
            PreparePlayersForMainMenu();

            var gameService = ServiceManager.Instance.Get<GameService>();

            if (gameService is null)
            {
                Logs.LogError("[LobbyReturnToMainMenuController] GameService is missing. Cannot return to main menu.");
                _isReturning = false;
                return;
            }

            await gameService.ReturnLobbyToMainMenuAsync();
        }

        private void PauseLobby()
        {
            Time.timeScale = 0f;

            var audioService = ServiceManager.Instance.Get<AudioService>();

            if (audioService is not null)
                audioService.SetPause(1);
        }

        private void ResumeLobby()
        {
            Time.timeScale = 1f;

            var audioService = ServiceManager.Instance.Get<AudioService>();

            if (audioService is not null)
                audioService.SetPause(0);
        }

        private void LockLobby()
        {
            if (_sandboxController)
                _sandboxController.LockAllPlayers();
        }

        private void ResetReadyState()
        {
            if (_readyController)
                _readyController.ResetReady();
        }

        private void DisableJoining()
        {
            if (!_disableJoining)
                return;

            if (PlayerInputBridge.Instance)
                PlayerInputBridge.Instance.CanJoin(false);
        }

        private void ClearActiveBombshells()
        {
            var bombshellSystem = SystemManager.Instance?.Get<BombshellSystem>();
            bombshellSystem?.ClearActiveBombshells();
        }

        private void PreparePlayersForMainMenu()
        {
            if (!_sandboxController)
                return;

            var players = _sandboxController.GetSpawnedPlayers();

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];

                if (!player)
                    continue;

                player.SetControlContext(PlayerControlContext.Menu);

                if (_despawnLobbyCharacters)
                    player.DespawnInGame();
            }
        }

        public bool CanHandleUIInput(PlayerManager player)
        {
            return _isConfirmationOpen &&
                   _activePlayer &&
                   ReferenceEquals(_activePlayer, player);
        }

        public bool HandleNavigate(PlayerManager player, Vector2 direction)
        {
            return false;
        }

        public bool HandleSubmit(PlayerManager player)
        {
            return false;
        }

        public bool HandleCancel(PlayerManager player)
        {
            CancelReturnToMainMenu();
            return true;
        }
    }
}