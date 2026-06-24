using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MortierFu
{
    public sealed class LobbyReturnToMainMenuController : MonoBehaviour
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

        [Header("Input")]
        [SerializeField] private string _cancelActionName = "Cancel";

        [Header("Options")]
        [SerializeField] private bool _despawnLobbyCharacters = true;
        [SerializeField] private bool _disableJoining = true;
        [SerializeField] private bool _onlyPlayerOneCanUse = false;

        private PlayerManager _activePlayer;
        private InputAction _cancelAction;

        private bool _isConfirmationOpen;
        private bool _isReturning;

        private void Awake()
        {
            ResolveReferences();

            if (_confirmPanel)
                _confirmPanel.SetActive(false);

            if (_confirmButton)
                _confirmButton.onClick.AddListener(ConfirmReturnToMainMenu);

            if (_cancelButton)
                _cancelButton.onClick.AddListener(CancelReturnToMainMenu);
        }

        private void OnDestroy()
        {
            UnbindInput();

            if (_confirmButton)
                _confirmButton.onClick.RemoveListener(ConfirmReturnToMainMenu);

            if (_cancelButton)
                _cancelButton.onClick.RemoveListener(CancelReturnToMainMenu);
        }

        private void ResolveReferences()
        {
            if (!_sandboxController)
                _sandboxController = GetComponent<LobbySandboxController>();

            if (!_stateController)
                _stateController = GetComponent<LobbySandboxStateController>();

            if (!_readyController)
                _readyController = GetComponent<LobbyStartReadyController>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isConfirmationOpen || _isReturning)
                return;

            PlayerCharacter character = ResolvePlayerCharacter(other);

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

            if (_stateController && _stateController.CurrentState != LobbySandboxState.Sandbox)
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
            _isConfirmationOpen = true;

            PauseLobby();

            if (_sandboxController)
                _sandboxController.LockAllPlayers();

            player.SetControlContext(PlayerControlContext.LobbyReturnConfirmationOwner);

            if (_confirmPanel)
                _confirmPanel.SetActive(true);

            BindInput(player);
            SelectDefaultButton();

            Logs.Log($"[LobbyReturnToMainMenuController] Confirmation opened by Player {player.PlayerIndex + 1}.");
        }

        private void BindInput(PlayerManager player)
        {
            UnbindInput();

            if (!player || !player.PlayerInput)
                return;

            var actions = player.PlayerInput.actions;

            _cancelAction = actions.FindAction(_cancelActionName, false);

            if (_cancelAction != null)
                _cancelAction.performed += OnCancel;
        }

        private void UnbindInput()
        {
            if (_cancelAction != null)
                _cancelAction.performed -= OnCancel;

            _cancelAction = null;
        }

        private void OnCancel(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed)
                return;

            CancelReturnToMainMenu();
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
            if (!_isConfirmationOpen)
                return;

            Logs.Log("[LobbyReturnToMainMenuController] Return to main menu canceled.");

            CloseConfirmation();
            ResumeLobby();
        }

        private void CloseConfirmation()
        {
            UnbindInput();

            if (_confirmPanel)
                _confirmPanel.SetActive(false);

            if (_sandboxController)
                _sandboxController.UnlockAllPlayers();

            if (_activePlayer)
                _sandboxController?.ApplyCurrentContextToPlayer(_activePlayer);

            _activePlayer = null;
            _isConfirmationOpen = false;
        }

        private async UniTaskVoid ReturnToMainMenuAsync()
        {
            _isReturning = true;

            Logs.Log("[LobbyReturnToMainMenuController] Returning to main menu from lobby.");

            UnbindInput();

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

            if (audioService != null)
                audioService.SetPause(1);
        }

        private void ResumeLobby()
        {
            Time.timeScale = 1f;

            var audioService = ServiceManager.Instance.Get<AudioService>();

            if (audioService != null)
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
    }
}