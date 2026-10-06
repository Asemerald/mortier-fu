using MortierFu.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MortierFu
{
    public sealed class UnityPlayerUISession
    {
        private PlayerManager _player;
        private EventSystem _eventSystem;
        private InputSystemUIInputModule _uiInputModule;
        private GameObject _firstSelected;

        private InputSystemUIInputModule _previousUiInputModule;
        private PlayerControlContext _previousContext;

        private InputActionAsset _previousActionsAsset;
        private InputActionReference _previousMove;
        private InputActionReference _previousSubmit;
        private InputActionReference _previousCancel;

        private InputActionReference _runtimeMove;
        private InputActionReference _runtimeSubmit;
        private InputActionReference _runtimeCancel;

        private bool _isActive;

        public void Begin(PlayerManager player, EventSystem eventSystem, InputSystemUIInputModule uiInputModule, Selectable firstSelected, PlayerControlContext context)
        {
            GameObject selectedObject = firstSelected ? firstSelected.gameObject : null;
            Begin(player, eventSystem, uiInputModule, selectedObject, context);
        }

        private void Begin(PlayerManager player, EventSystem eventSystem, InputSystemUIInputModule uiInputModule, GameObject firstSelected, PlayerControlContext context)
        {
            End();

            if (!player || !player.PlayerInput || !eventSystem || !uiInputModule)
            {
                Logs.LogError("[UnityPlayerUISession] Cannot begin UI session because references are missing.");
                return;
            }

            _player = player;
            _eventSystem = eventSystem;
            _uiInputModule = uiInputModule;
            _firstSelected = firstSelected;

            _previousContext = player.ControlContext;

            _previousUiInputModule = player.PlayerInput.uiInputModule == uiInputModule ? null : player.PlayerInput.uiInputModule;

            CaptureInputModuleState();

            _eventSystem.enabled = true;
            _eventSystem.SetSelectedGameObject(null);

            _player.PlayerInput.uiInputModule = _uiInputModule;
            _player.SetControlContext(context);

            BindNativeUIModuleToPlayer(_player);

            _player.SetUnityEventSystemUIActive(true);

            if (_firstSelected)
                _eventSystem.SetSelectedGameObject(_firstSelected);

            _isActive = true;
        }

        public void End(bool restorePlayerContext = true)
        {
            if (!_isActive)
                return;

            if (_eventSystem)
                _eventSystem.SetSelectedGameObject(null);

            if (_player)
            {
                _player.SetUnityEventSystemUIActive(false);
                _player.PlayerInput.uiInputModule = _previousUiInputModule;

                if (restorePlayerContext)
                    _player.SetControlContext(_previousContext);
            }

            RestoreInputModuleState();
            DestroyRuntimeActionReferences();

            _player = null;
            _eventSystem = null;
            _uiInputModule = null;
            _firstSelected = null;
            _previousUiInputModule = null;

            _isActive = false;
        }

        private void BindNativeUIModuleToPlayer(PlayerManager player)
        {
            if (!_uiInputModule || !player || !player.PlayerInput || !player.PlayerInput.actions)
                return;

            InputActionAsset actions = player.PlayerInput.actions;
            InputActionMap uiMap = actions.FindActionMap(PlayerInputActionNames.UIMap, throwIfNotFound: false);

            if (uiMap == null)
            {
                Logs.LogError($"[UnityPlayerUISession] UI action map not found for Player {player.PlayerIndex + 1}.");
                return;
            }

            DestroyRuntimeActionReferences();

            _runtimeMove = CreateUIActionReference(uiMap, PlayerInputActionNames.Navigate);
            _runtimeSubmit = CreateUIActionReference(uiMap, PlayerInputActionNames.Submit);
            _runtimeCancel = CreateUIActionReference(uiMap, PlayerInputActionNames.Cancel);

            _uiInputModule.actionsAsset = actions;

            _uiInputModule.move = _runtimeMove;
            _uiInputModule.submit = _runtimeSubmit;
            _uiInputModule.cancel = _runtimeCancel;

            _uiInputModule.enabled = false;
            _uiInputModule.enabled = true;
        }

        private static InputActionReference CreateUIActionReference(InputActionMap uiMap, string actionName)
        {
            InputAction action = uiMap.FindAction(actionName, throwIfNotFound: false);

            if (action != null)
                return InputActionReference.Create(action);

            Logs.LogError($"[UnityPlayerUISession] UI action '{actionName}' not found.");
            return null;
        }

        private void CaptureInputModuleState()
        {
            if (!_uiInputModule)
                return;

            _previousActionsAsset = _uiInputModule.actionsAsset;
            _previousMove = _uiInputModule.move;
            _previousSubmit = _uiInputModule.submit;
            _previousCancel = _uiInputModule.cancel;
        }

        private void RestoreInputModuleState()
        {
            if (!_uiInputModule)
                return;

            _uiInputModule.actionsAsset = _previousActionsAsset;
            _uiInputModule.move = _previousMove;
            _uiInputModule.submit = _previousSubmit;
            _uiInputModule.cancel = _previousCancel;

            _previousActionsAsset = null;
            _previousMove = null;
            _previousSubmit = null;
            _previousCancel = null;
        }

        private void DestroyRuntimeActionReferences()
        {
            if (_runtimeMove)
                Object.Destroy(_runtimeMove);

            if (_runtimeSubmit)
                Object.Destroy(_runtimeSubmit);

            if (_runtimeCancel)
                Object.Destroy(_runtimeCancel);

            _runtimeMove = null;
            _runtimeSubmit = null;
            _runtimeCancel = null;
        }
    }
}