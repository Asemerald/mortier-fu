using MortierFu.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
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

        private bool _isActive;

        public bool IsActive => _isActive;

        public void Begin(PlayerManager player, EventSystem eventSystem, InputSystemUIInputModule uiInputModule, Selectable firstSelected, PlayerControlContext context)
        {
            GameObject selectedObject = firstSelected ? firstSelected.gameObject : null;

            Begin(player, eventSystem, uiInputModule, selectedObject, context);
        }

        public void Begin(PlayerManager player, EventSystem eventSystem, InputSystemUIInputModule uiInputModule, GameObject firstSelected, PlayerControlContext context)
        {
            End();

            if (!player || !eventSystem || !uiInputModule)
            {
                Logs.LogError("[UnityPlayerUISession] Cannot begin UI session because references are missing.");
                return;
            }

            _player = player;
            _eventSystem = eventSystem;
            _uiInputModule = uiInputModule;
            _firstSelected = firstSelected;

            _previousContext = player.ControlContext;
            _previousUiInputModule = player.PlayerInput.uiInputModule;

            _eventSystem.enabled = true;
            _uiInputModule.enabled = true;

            _eventSystem.SetSelectedGameObject(null);

            _player.PlayerInput.uiInputModule = _uiInputModule;
            _player.SetUnityEventSystemUIActive(true);
            _player.SetControlContext(context);

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

            _player = null;
            _eventSystem = null;
            _uiInputModule = null;
            _firstSelected = null;
            _previousUiInputModule = null;

            _isActive = false;
        }
    }
}