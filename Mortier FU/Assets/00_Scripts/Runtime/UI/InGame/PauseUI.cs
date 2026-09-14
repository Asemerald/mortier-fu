using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using System.Threading;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public enum PauseUISceneContext
    {
        Lobby,
        Gameplay
    }

    public sealed class PauseUI : MonoBehaviour
    {
        private struct PlayerPauseSnapshot
        {
            public PlayerManager Player;
            public PlayerControlContext Context;
            public InputSystemUIInputModule UiInputModule;
            public bool UnityEventSystemUIActive;
        }

        [Header("Mode")]
        [SerializeField] private PauseUISceneContext _sceneContext = PauseUISceneContext.Lobby;

        [Header("Root")]
        [SerializeField] private GameObject _root;
        [SerializeField] private GameObject _pausePanel;
        [SerializeField] private GameObject _pauseBackground;
        [SerializeField] private GameObject _blackPanel;

        [Header("Sub Panels")]
        [SerializeField] private GameObject _settingsPanel;

        [Header("Buttons")]
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _primaryActionButton;
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private Button _resetTutorialButton;
        
        [Header("Button Text")]
        [SerializeField] private TMP_Text _settingsButtonText;
        [SerializeField] private TMP_Text _resumeButtonText;
        [SerializeField] private TMP_Text _primaryActionButtonText;
        [SerializeField] private TMP_Text _mainMenuButtonText;

        [Header("Settings UI")]
        [SerializeField] private Toggle _fullscreenToggle;
        [SerializeField] private Toggle _vSyncToggle;
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;
        [SerializeField] private Slider _ambienceVolumeSlider;

        [Header("Unity UI")]
        [SerializeField] private EventSystem _eventSystem;
        [SerializeField] private InputSystemUIInputModule _uiInputModule;
        [SerializeField] private UIConfirmationModalController _confirmationModal;

        [Header("Confirmation Text")]
        [SerializeField] private string _returnToMainMenuDescription = "Are you sure you want to return to Main Menu?";
        [SerializeField] private string _endGameDescription = "Are you sure you want to end the Game?";
        [SerializeField] private string _quitGameDescription = "Are you sure you want to quit the Game?";
        [SerializeField] private string _confirmLabel = "Confirm";
        [SerializeField] private string _cancelLabel = "Cancel";
        
        [Header("Switch by Input Element")]
        [SerializeField] private GameObject[] _buttonKeyboard;
        [SerializeField] private GameObject[] _buttonGamepad;

        [Header("Animation")] 
        [SerializeField, Min(0f)] private float _buttonAnimationDuration = 0.3f;

        private readonly List<PlayerPauseSnapshot> _snapshots = new();

        private GamePauseSystem _gamePauseSystem;
        private GameService _gameService;
        private LobbyService _lobbyService;
        private ShakeService _shakeService;

        private PlayerManager _owner;
        private bool _isOpen;
        
        private CancellationTokenSource _initializationCancellation;

        private bool _pauseSystemEventsBound;
        private bool _settingsInitialized;
        
        private SaveService _saveService;

        private void Awake()
        {
            ResolveReferences();
            ApplyLabels();
            HideInstant();
        }

        private void OnEnable()
        {
            BindButtonEvents();
            BindSettingsFeedbackEvents();
            StartInitialization();
        }

        private void OnDisable()
        {
            StopInitialization();

            UnbindPauseSystemEvents();
            UnbindButtonEvents();
            UnbindSettingsFeedbackEvents();

            if (_isOpen)
                ClosePauseUI(restorePlayers: true);
        }

        private void OnDestroy()
        {
            StopInitialization();

            UnbindPauseSystemEvents();
            UnbindButtonEvents();
            UnbindSettingsFeedbackEvents();
        }

        private void ResolveReferences()
        {
            _gameService = ServiceManager.Instance?.Get<GameService>();
            _lobbyService = ServiceManager.Instance?.Get<LobbyService>();
            _shakeService = ServiceManager.Instance?.Get<ShakeService>();
            _saveService = ServiceManager.Instance?.Get<SaveService>();

            TryResolveGamePauseSystem();

            if (!_eventSystem)
                _eventSystem = EventSystem.current;

            if (!_uiInputModule && _eventSystem)
                _uiInputModule = _eventSystem.GetComponent<InputSystemUIInputModule>();
        }
        
        private bool TryResolveGamePauseSystem()
        {
            if (_gamePauseSystem is not null)
                return true;

            if (SystemManager.Instance is null)
                return false;

            _gamePauseSystem = SystemManager.Instance.Get<GamePauseSystem>();

            return _gamePauseSystem is not null;
        }
        
        private void StartInitialization()
        {
            StopInitialization();

            _initializationCancellation = new CancellationTokenSource();
            InitializeAsync(_initializationCancellation.Token).Forget();
        }

        private void StopInitialization()
        {
            _initializationCancellation?.Cancel();
            _initializationCancellation?.Dispose();
            _initializationCancellation = null;
        }

        private async UniTaskVoid InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!TryResolveGamePauseSystem() || !_gamePauseSystem.AreSettingsReady)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();

                BindPauseSystemEvents();
                InitializeSettingsUI();
            }
            catch (OperationCanceledException)
            { }
        }

        private void InitializeSettingsUI()
        {
            if (_settingsInitialized)
                return;

            if (_gamePauseSystem is null)
            {
                Logs.LogError("[PauseUI] GamePauseSystem is missing.");
                return;
            }

            _gamePauseSystem.RestoreSettingsFromSave();

            _gamePauseSystem.UpdateUIFromSave(_fullscreenToggle, _vSyncToggle, _masterVolumeSlider, _musicVolumeSlider, _sfxVolumeSlider, _ambienceVolumeSlider);

            _gamePauseSystem.BindUIEvents(_fullscreenToggle, _vSyncToggle, _masterVolumeSlider, _musicVolumeSlider, _sfxVolumeSlider, _ambienceVolumeSlider);

            _settingsInitialized = true;
        }

        private void ApplyLabels()
        {
            if (_settingsButtonText)
                _settingsButtonText.text = "Settings";

            if (_resumeButtonText)
                _resumeButtonText.text = "Resume";

            if (_primaryActionButtonText)
                _primaryActionButtonText.text = _sceneContext == PauseUISceneContext.Lobby
                    ? "Quit Game"
                    : "End Game";

            if (_mainMenuButtonText)
                _mainMenuButtonText.text = "Main menu";
        }

        private void BindPauseSystemEvents()
        {
            if (_pauseSystemEventsBound)
                return;

            if (_gamePauseSystem is null)
                return;

            _gamePauseSystem.Paused -= HandlePaused;
            _gamePauseSystem.Resumed -= HandleResumed;

            _gamePauseSystem.Paused += HandlePaused;
            _gamePauseSystem.Resumed += HandleResumed;

            _pauseSystemEventsBound = true;
        }

        private void UnbindPauseSystemEvents()
        {
            if (!_pauseSystemEventsBound)
                return;

            if (_gamePauseSystem is not null)
            {
                _gamePauseSystem.Paused -= HandlePaused;
                _gamePauseSystem.Resumed -= HandleResumed;
            }

            _pauseSystemEventsBound = false;
        }

        private void BindButtonEvents()
        {
            if (_settingsButton)
            {
                _settingsButton.onClick.RemoveListener(OpenSettingsPanel);
                _settingsButton.onClick.AddListener(OpenSettingsPanel);
            }
            
            if (_resumeButton)
            {
                _resumeButton.onClick.RemoveListener(ResumeGame);
                _resumeButton.onClick.AddListener(ResumeGame);
            }

            if (_primaryActionButton) 
            {
                _primaryActionButton.onClick.RemoveListener(OpenPrimaryActionConfirmation);
                _primaryActionButton.onClick.AddListener(OpenPrimaryActionConfirmation);
            }

            if (_mainMenuButton)
            {
                _mainMenuButton.onClick.RemoveListener(OpenMainMenuConfirmation);
                _mainMenuButton.onClick.AddListener(OpenMainMenuConfirmation);
            }

            if (!_resetTutorialButton) return;
            _resetTutorialButton.onClick.RemoveListener(_saveService.ResetTutorial);
            _resetTutorialButton.onClick.AddListener(_saveService.ResetTutorial);
        }

        private void UnbindButtonEvents()
        {
            if (_settingsButton)
                _settingsButton.onClick.RemoveListener(OpenSettingsPanel);

            if (_resumeButton)
                _resumeButton.onClick.RemoveListener(ResumeGame);

            if (_primaryActionButton)
                _primaryActionButton.onClick.RemoveListener(OpenPrimaryActionConfirmation);

            if (_mainMenuButton)
                _mainMenuButton.onClick.RemoveListener(OpenMainMenuConfirmation);
            
            if (_resetTutorialButton)
                _resetTutorialButton.onClick.RemoveListener(_saveService.ResetTutorial);
        }

        private void BindSettingsFeedbackEvents()
        {
            if (_fullscreenToggle)
                _fullscreenToggle.onValueChanged.AddListener(PlayToggleFeedback);

            if (_vSyncToggle)
                _vSyncToggle.onValueChanged.AddListener(PlayToggleFeedback);

            if (_masterVolumeSlider)
                _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

            if (_musicVolumeSlider)
                _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

            if (_sfxVolumeSlider)
                _sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            
            if (_ambienceVolumeSlider)
                _ambienceVolumeSlider.onValueChanged.AddListener(OnAmbienceVolumeChanged);
        }

        private void UnbindSettingsFeedbackEvents()
        {
            if (_fullscreenToggle)
                _fullscreenToggle.onValueChanged.RemoveListener(PlayToggleFeedback);

            if (_vSyncToggle)
                _vSyncToggle.onValueChanged.RemoveListener(PlayToggleFeedback);

            if (_masterVolumeSlider)
                _masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);

            if (_musicVolumeSlider)
                _musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);

            if (_sfxVolumeSlider)
                _sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            
            if (_ambienceVolumeSlider)
                _ambienceVolumeSlider.onValueChanged.RemoveListener(OnAmbienceVolumeChanged);
        }

        private void HandlePaused(PlayerManager player)
        {
            if (_isOpen)
                return;

            if (!player)
            {
                Logs.LogError("[PauseUI] Cannot open pause without owner.");
                _gamePauseSystem?.Resume();
                return;
            }

            if (!_eventSystem || !_uiInputModule)
            {
                Logs.LogError("[PauseUI] EventSystem or InputSystemUIInputModule is missing.");
                _gamePauseSystem?.Resume();
                return;
            }

            _owner = player;
            _isOpen = true;

            CapturePlayers();
            ApplyPauseInputState();
            UpdateUiImageByInput(_owner);
            
            ShowMainPanel();
            Select(_resumeButton);

            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Pause, 0);
            ServiceManager.Instance.Get<AudioService>()?.SetPause(1);
            _shakeService?.ShakeController(_owner, ShakeService.ShakeType.MID);
        }

        private void UpdateUiImageByInput(PlayerManager ownerInput)
        {
            bool isKeyboard = ownerInput.IsKeyboardAndMouseControlScheme();

            foreach (var buttonKeyboard in _buttonKeyboard)
                buttonKeyboard.SetActive(isKeyboard);
            
            foreach (var buttonGamepad in _buttonGamepad)
                buttonGamepad.SetActive(!isKeyboard);
        }

        private void HandleResumed()
        {
            if (!_isOpen)
                return;

            ClosePauseUI(restorePlayers: true);

            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Return, 0);
            ServiceManager.Instance.Get<AudioService>()?.SetPause(0);

            if (_owner)
                _shakeService?.ShakeController(_owner, ShakeService.ShakeType.MID);
        }

        private void CapturePlayers()
        {
            _snapshots.Clear();

            IReadOnlyList<PlayerManager> players = _lobbyService?.GetPlayers();

            if (players == null)
            {
                CaptureSingleOwner();
                return;
            }

            for (int i = 0; i < players.Count; i++)
            {
                PlayerManager player = players[i];

                if (!player)
                    continue;

                _snapshots.Add(new PlayerPauseSnapshot
                {
                    Player = player,
                    Context = player.ControlContext,
                    UiInputModule = player.PlayerInput.uiInputModule,
                    UnityEventSystemUIActive = player.IsUnityEventSystemUIActive
                });
            }

            if (!ContainsSnapshotFor(_owner))
                CaptureSingleOwner();
        }

        private void CaptureSingleOwner()
        {
            if (!_owner)
                return;

            _snapshots.Add(new PlayerPauseSnapshot
            {
                Player = _owner,
                Context = _owner.ControlContext,
                UiInputModule = _owner.PlayerInput.uiInputModule,
                UnityEventSystemUIActive = _owner.IsUnityEventSystemUIActive
            });
        }

        private bool ContainsSnapshotFor(PlayerManager player)
        {
            if (!player)
                return false;

            for (int i = 0; i < _snapshots.Count; i++)
            {
                if (ReferenceEquals(_snapshots[i].Player, player))
                    return true;
            }

            return false;
        }

        private void ApplyPauseInputState()
        {
            // Clean de l'ancien owner, blockage des autres joueurs et attribution de l'InputSystemUIInputModule à celui qui a initié la pause.
            DetachPauseInputModuleFromCapturedPlayers();

            for (int i = 0; i < _snapshots.Count; i++)
            {
                PlayerManager player = _snapshots[i].Player;

                if (!player || player == _owner)
                    continue;

                ApplyBlockedInput(player);
            }

            ApplyOwnerPauseInput(_owner);
        }
        
        private void DetachPauseInputModuleFromCapturedPlayers()
        {
            for (int i = 0; i < _snapshots.Count; i++)
            {
                PlayerManager player = _snapshots[i].Player;

                if (!player || !player.PlayerInput)
                    continue;
                
                // L'InputSystemUIInputModule est partagé, il faut donc retirer les anciennes attributions avant de donner le nouvel ownership.
                if (player.PlayerInput.uiInputModule == _uiInputModule)
                    player.PlayerInput.uiInputModule = null;
            }
        }

        private void ApplyOwnerPauseInput(PlayerManager player)
        {
            if (!player)
                return;

            if (player.PlayerInput)
                player.PlayerInput.uiInputModule = _uiInputModule;

            player.SetControlContext(PlayerControlContext.PauseMenu);

            BindNativeUIModuleToOwner(player);

            player.SetUnityEventSystemUIActive(true);
        }

        private static void ApplyBlockedInput(PlayerManager player)
        {
            if (!player)
                return;

            // UIBlocked, hmmm je pense il y un indice dans le nom, mais ça bloque notre système d'input custom (jure?). Il faut obligatoirement bloquer également le système natif UI d'Unity.
            if (player.PlayerInput)
                player.PlayerInput.uiInputModule = null;

            player.SetControlContext(PlayerControlContext.UIBlocked);
            player.SetUnityEventSystemUIActive(true);
        }

        private void RestorePlayers()
        {
            for (int i = 0; i < _snapshots.Count; i++)
            {
                PlayerPauseSnapshot snapshot = _snapshots[i];

                if (!snapshot.Player)
                    continue;

                if (snapshot.Player.PlayerInput)
                {
                    // Il ne faut pas restaurer l'InputSystemUIInputModule partagé comme état permanent d'un joueur.
                    InputSystemUIInputModule moduleToRestore = snapshot.UiInputModule == _uiInputModule ? null : snapshot.UiInputModule;

                    snapshot.Player.PlayerInput.uiInputModule = moduleToRestore;
                }

                snapshot.Player.SetControlContext(snapshot.Context);
                snapshot.Player.SetUnityEventSystemUIActive(snapshot.UnityEventSystemUIActive);
            }

            _snapshots.Clear();
        }

        private void ShowMainPanel()
        {
            if (_root)
                _root.SetActive(true);

            if (_pauseBackground)
                _pauseBackground.SetActive(true);

            if (_blackPanel)
                _blackPanel.SetActive(true);

            if (_pausePanel)
                _pausePanel.SetActive(true);

            if (_settingsPanel)
                _settingsPanel.SetActive(false);
        }

        private void OpenSettingsPanel()
        {
            PlayPanelSelectionFeedback();

            if (_pausePanel)
                _pausePanel.SetActive(false);

            if (_settingsPanel)
                _settingsPanel.SetActive(true);

            Select(_fullscreenToggle);
        }
        
        public void ReturnToMainPanelFromSubPanel(Selectable returnSelection)
        {
            PlayPanelSelectionFeedback();

            ShowMainPanel();

            if (returnSelection)
                Select(returnSelection);
            else
                Select(_settingsButton);
        }

        public void ReturnToMainPanelFromSubPanel() => ReturnToMainPanelFromSubPanel(_settingsButton);

        private void ResumeGame() => ResumeGameAsync().Forget();

        private async UniTask ResumeGameAsync()
        {
            PlayPanelSelectionFeedback();

            await UniTask.Delay(TimeSpan.FromSeconds(_buttonAnimationDuration), ignoreTimeScale: true);
                
            _gamePauseSystem?.Resume();
        }

        private void OpenMainMenuConfirmation() =>
            OpenPauseConfirmation(_returnToMainMenuDescription, ConfirmReturnToMainMenuAsync, _mainMenuButton);
        
        private void OpenPrimaryActionConfirmation()
        {
            if (_sceneContext == PauseUISceneContext.Lobby)
                OpenPauseConfirmation(_quitGameDescription, ConfirmQuitGameAsync, _primaryActionButton);
            else
                OpenPauseConfirmation(_endGameDescription, ConfirmEndGameAsync, _primaryActionButton);
        }
            

        private void OpenPauseConfirmation(string description, Func<UniTask> onConfirmAsync, Selectable returnSelection)
        {
            if (!_confirmationModal || !_owner)
                return;

            PlayPanelSelectionFeedback();

            UIConfirmationRequest request = new(
                owner: _owner,
                description: description,
                confirmLabel: _confirmLabel,
                cancelLabel: _cancelLabel,
                onConfirmAsync: onConfirmAsync,
                onCancelAfterCloseAsync: () => RestorePauseSelectionAsync(returnSelection),
                pauseGameWhileOpen: false,
                lockPlayersWhileOpen: true,
                restoreContextOnConfirm: true,
                resumeTimeScaleOnConfirm: false,
                ownerContext: PlayerControlContext.PauseConfirmationOwner
            );

            _confirmationModal.TryOpen(request);
        }

        private UniTask RestorePauseSelectionAsync(Selectable selectable)
        {
            if (_owner)
                ApplyOwnerPauseInput(_owner);

            ShowMainPanel();
            Select(selectable);

            return UniTask.CompletedTask;
        }

        private async UniTask ConfirmReturnToMainMenuAsync()
        {
            await EndPauseForTransitionAsync();

            PlayerInputBridge.Instance?.CanJoin(false);

            BombshellSystem bombshellSystem = SystemManager.Instance?.Get<BombshellSystem>();
            bombshellSystem?.ClearActiveBombshells();

            if (_gameService is null)
            {
                Logs.LogError("[PauseUI] GameService is missing. Cannot return to main menu.");
                return;
            }

            if (_sceneContext == PauseUISceneContext.Lobby)
                await _gameService.ReturnLobbyToMainMenuAsync();
            else
                await _gameService.ReturnToMainMenuAsync();
        }

        private async UniTask ConfirmEndGameAsync()
        {
            await EndPauseForTransitionAsync();

            if (_gameService is null)
            {
                Logs.LogError("[PauseUI] GameService is missing. Cannot return to lobby.");
                return;
            }

            _gameService.ReturnToLobby();
        }

        private async UniTask ConfirmQuitGameAsync()
        {
            await EndPauseForTransitionAsync();
            Application.Quit();
        }

        private async UniTask EndPauseForTransitionAsync()
        {
            if (_gamePauseSystem is not null && _gamePauseSystem.IsPaused)
                _gamePauseSystem.Resume();

            await UniTask.Delay(TimeSpan.FromSeconds(_buttonAnimationDuration));
        }

        private void ClosePauseUI(bool restorePlayers)
        {
            _confirmationModal?.ForceCloseInstant(restorePlayers: false);

            ClearSelectedObject();
            _gamePauseSystem?.SaveSettings();
            HideInstant();

            if (restorePlayers)
                RestorePlayers();
            else
                _snapshots.Clear();

            _owner = null;
            _isOpen = false;
        }

        private void HideInstant()
        {
            if (_pausePanel)
                _pausePanel.SetActive(false);

            if (_pauseBackground)
                _pauseBackground.SetActive(false);

            if (_blackPanel)
                _blackPanel.SetActive(false);

            if (_settingsPanel)
                _settingsPanel.SetActive(false);

            if (_root)
                _root.SetActive(false);
        }

        private void Select(Selectable selectable) => Select(selectable ? selectable.gameObject : null);

        private void Select(GameObject selectedObject)
        {
            if (!_eventSystem || !selectedObject)
                return;

            if (!selectedObject.activeInHierarchy)
                return;

            _eventSystem.SetSelectedGameObject(null);
            _eventSystem.SetSelectedGameObject(selectedObject);
        }

        private void ClearSelectedObject()
        {
            if (_eventSystem)
                _eventSystem.SetSelectedGameObject(null);
        }

        private void PlayPanelSelectionFeedback()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Select);

            if (_owner)
                _shakeService?.ShakeController(_owner, ShakeService.ShakeType.MID);
        }

        private void PlayToggleFeedback(bool value) => PlayMinorUIFeedback();

        private void OnMasterVolumeChanged(float value)
        {
            AudioService.SetVolume(AudioService.BusEnum.MASTER, value);
            PlayMinorUIFeedback();
        }

        private void OnMusicVolumeChanged(float value)
        {
            AudioService.SetVolume(AudioService.BusEnum.MUSIC, value);
            PlayMinorUIFeedback();
        }

        private void OnSfxVolumeChanged(float value)
        {
            AudioService.SetVolume(AudioService.BusEnum.SFX, value);
            PlayMinorUIFeedback();
        }
        
        private void OnAmbienceVolumeChanged(float value)
        {
            AudioService.SetVolume(AudioService.BusEnum.AMBIENCE, value);
            PlayMinorUIFeedback();
        }

        private void PlayMinorUIFeedback()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Slider);

            if (_owner)
                _shakeService?.ShakeController(_owner, ShakeService.ShakeType.LITTLE);
        }
        
        public void HandleMainPanelCancel()
        {
            if (!_isOpen)
                return;

            if (_confirmationModal && _confirmationModal.IsActive)
            {
                _confirmationModal.RequestCancelFromInput();
                return;
            }

            if (!IsMainPausePanelActive())
                return;

            _gamePauseSystem?.Resume();
        }
        
        private bool IsMainPausePanelActive()
        {
            if (!_pausePanel || !_pausePanel.activeInHierarchy)
                return false;

            if (_settingsPanel && _settingsPanel.activeInHierarchy)
                return false;
            
            return  true;
        }
        
        private void BindNativeUIModuleToOwner(PlayerManager player)
        {
            if (!_uiInputModule || !player || !player.PlayerInput || !player.PlayerInput.actions)
                return;

            // Chaque PlayerInput possède sa propre copie RUNTIME de l'InputActionAsset.
            // L'InputSystemUIInputModule doit donc lire la copie du joueur qui contrôle réellement la pause.
            InputActionAsset actions = player.PlayerInput.actions;
            InputActionMap uiMap = actions.FindActionMap(PlayerInputActionNames.UIMap, throwIfNotFound: false);

            if (uiMap == null)
            {
                Logs.LogError($"[PauseUI] UI action map not found for Player {player.PlayerIndex + 1}.");
                return;
            }

            _uiInputModule.actionsAsset = actions;
            
            // Changer l'asset ne doit, certainement, pas suffire, c'est possible que les références Move/Submit/Cancel peuvent rester vides, donc on va les recréer nous même pour être sûr à 100%.
            _uiInputModule.move = CreateUIActionReference(uiMap, PlayerInputActionNames.Navigate);
            _uiInputModule.submit = CreateUIActionReference(uiMap, PlayerInputActionNames.Submit);
            _uiInputModule.cancel = CreateUIActionReference(uiMap, PlayerInputActionNames.Cancel);

            // Force le module à reconstruire ses bindings internes après le changement d'actions.
            _uiInputModule.enabled = false;
            _uiInputModule.enabled = true;
        }

        private static InputActionReference CreateUIActionReference(InputActionMap uiMap, string actionName)
        {
            InputAction action = uiMap.FindAction(actionName, throwIfNotFound: false);

            if (action == null)
            {
                Logs.LogError($"[PauseUI] UI action '{actionName}' not found.");
                return null;
            }
            
            // Force la référence à pointer vers l'action runtime de l'owner.
            return InputActionReference.Create(action);
        }
    }
}