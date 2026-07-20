using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;

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

        [Header("Navigation")]
        [SerializeField] private float _navigationDeadZone = 0.5f;

        private PlayerManager _activePlayer;
        private PlayerControlContext _previousActivePlayerContext;

        private Selectable _currentSelectable;

        private bool _isConfirmationOpen;
        private bool _isReturning;
        private bool _navigationWasPressed;
        
        private InputSystemUIInputModule _uiInputModule;
        private bool _hasStoredUiInputModuleState;
        private bool _previousUiInputModuleEnabled;

        private PlayerUIInputService UIInputService =>
            ServiceManager.Instance?.Get<PlayerUIInputService>();

        private void Awake()
        {
            ValidateReferences();
            HideConfirmationPanel();
            BindButtons();
        }

        private void OnDestroy()
        {
            UIInputService?.RemoveFromAll(this);
            
            RestoreGlobalUIInputModule();
            UnbindButtons();
            ResumeLobby();

            _activePlayer = null;
            _currentSelectable = null;
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

        private void ValidateReferences()
        {
            if (!_sandboxController)
                Logs.LogWarning("[LobbyReturnToMainMenuController] SandboxController reference is missing.", this);

            if (!_stateController)
                Logs.LogWarning("[LobbyReturnToMainMenuController] SandboxStateController reference is missing.", this);

            if (!_readyController)
                Logs.LogWarning("[LobbyReturnToMainMenuController] ReadyController reference is missing.", this);

            if (!_confirmPanel)
                Logs.LogWarning("[LobbyReturnToMainMenuController] ConfirmPanel reference is missing.", this);

            if (!_confirmButton)
                Logs.LogWarning("[LobbyReturnToMainMenuController] ConfirmButton reference is missing.", this);

            if (!_cancelButton)
                Logs.LogWarning("[LobbyReturnToMainMenuController] CancelButton reference is missing.", this);
        }

        private void BindButtons()
        {
            if (_confirmButton)
                _confirmButton.onClick.AddListener(ConfirmReturnToMainMenu);

            if (_cancelButton)
                _cancelButton.onClick.AddListener(CancelReturnToMainMenu);
        }

        private void UnbindButtons()
        {
            if (_confirmButton)
                _confirmButton.onClick.RemoveListener(ConfirmReturnToMainMenu);

            if (_cancelButton)
                _cancelButton.onClick.RemoveListener(CancelReturnToMainMenu);
        }
        
        private void DisableGlobalUIInputModule()
        {
            var eventSystem = EventSystem.current;

            if (!eventSystem)
                return;

            if (!_uiInputModule)
                _uiInputModule = eventSystem.GetComponent<InputSystemUIInputModule>();

            if (!_uiInputModule)
                return;

            if (!_hasStoredUiInputModuleState)
            {
                _previousUiInputModuleEnabled = _uiInputModule.enabled;
                _hasStoredUiInputModuleState = true;
            }

            _uiInputModule.enabled = false;
        }

        private void RestoreGlobalUIInputModule()
        {
            if (!_hasStoredUiInputModuleState)
                return;

            if (_uiInputModule)
                _uiInputModule.enabled = _previousUiInputModuleEnabled;

            _hasStoredUiInputModuleState = false;
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

            if (_stateController && !_stateController.CanUseStartTarget())
                return;

            if (!IsPlayerInSandbox(player))
                return;

            OpenConfirmation(player);
        }

        private bool IsPlayerInSandbox(PlayerManager player)
        {
            if (!player || !_sandboxController)
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
            _navigationWasPressed = false;

            PauseLobby();
            DisableGlobalUIInputModule();

            if (_sandboxController)
                _sandboxController.LockAllPlayers();

            player.SetControlContext(PlayerControlContext.LobbyReturnConfirmationOwner);

            UIInputService?.Push(player, this);

            ShowConfirmationPanel();
            SelectDefaultButton();

            Logs.Log($"[LobbyReturnToMainMenuController] Confirmation opened by Player {player.PlayerIndex + 1}.");
        }

        private void ShowConfirmationPanel()
        {
            if (_confirmPanel)
                _confirmPanel.SetActive(true);
        }

        private void HideConfirmationPanel()
        {
            if (_confirmPanel)
                _confirmPanel.SetActive(false);
        }

        private void SelectDefaultButton()
        {
            Selectable selectable = _defaultSelectedButton;

            if (!selectable)
                selectable = _confirmButton;

            if (!selectable)
                selectable = _cancelButton;

            Select(selectable);
        }

        private void Select(Selectable selectable)
        {
            if (!selectable)
                return;

            _currentSelectable = selectable;

            EventSystem.current?.SetSelectedGameObject(selectable.gameObject);
        }

        public void ConfirmReturnToMainMenu()
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

            HideConfirmationPanel();
            RestoreGlobalUIInputModule();

            if (_sandboxController)
                _sandboxController.UnlockAllPlayers();

            RestoreActivePlayerContext();

            _activePlayer = null;
            _currentSelectable = null;
            _isConfirmationOpen = false;
            _navigationWasPressed = false;
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

            HideConfirmationPanel();
            RestoreGlobalUIInputModule();
            ResumeLobby();

            LockLobby();
            ResetReadyState();
            DisableJoining();
            ClearActiveBombshells();
            PreparePlayersForMainMenu();

            var gameService = ServiceManager.Instance.Get<GameService>();

            if (gameService is null)
            {
                Logs.LogError("[LobbyReturnToMainMenuController] GameService is missing. Cannot return to main menu.", this);
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
                   !_isReturning &&
                   _activePlayer &&
                   ReferenceEquals(_activePlayer, player) &&
                   player.CurrentPermissions.CanNavigateUI &&
                   player.CurrentPermissions.CanConfirmUI &&
                   player.CurrentPermissions.CanCancelUI;
        }

        public bool HandleNavigate(PlayerManager player, Vector2 direction)
        {
            if (!CanHandleUIInput(player))
                return false;

            if (direction.sqrMagnitude < _navigationDeadZone * _navigationDeadZone)
            {
                _navigationWasPressed = false;
                return true;
            }

            if (_navigationWasPressed)
                return true;

            _navigationWasPressed = true;

            Navigate(direction);
            return true;
        }

        public bool HandleSubmit(PlayerManager player)
        {
            if (!CanHandleUIInput(player))
                return false;

            SubmitCurrentSelection();
            return true;
        }

        public bool HandleCancel(PlayerManager player)
        {
            if (!CanHandleUIInput(player))
                return false;

            CancelReturnToMainMenu();
            return true;
        }

        private void Navigate(Vector2 direction)
        {
            if (!_currentSelectable)
                SelectDefaultButton();

            if (!_currentSelectable)
                return;

            Selectable nextSelectable = null;

            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            {
                nextSelectable = direction.x > 0f
                    ? _currentSelectable.FindSelectableOnRight()
                    : _currentSelectable.FindSelectableOnLeft();
            }
            else
            {
                nextSelectable = direction.y > 0f
                    ? _currentSelectable.FindSelectableOnUp()
                    : _currentSelectable.FindSelectableOnDown();
            }

            if (!nextSelectable)
                nextSelectable = GetFallbackSelectable();

            Select(nextSelectable);
        }

        private Selectable GetFallbackSelectable()
        {
            if (_currentSelectable == _confirmButton && _cancelButton)
                return _cancelButton;

            return _confirmButton ? _confirmButton : _cancelButton;
        }

        private void SubmitCurrentSelection()
        {
            if (!_currentSelectable)
                SelectDefaultButton();

            if (!_currentSelectable)
                return;

            if (_currentSelectable is Button button)
            {
                button.onClick.Invoke();
                return;
            }

            if (_confirmButton)
                _confirmButton.onClick.Invoke();
        }
    }
}