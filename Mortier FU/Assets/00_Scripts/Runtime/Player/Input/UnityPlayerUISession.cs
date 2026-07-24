using log4net.Appender;
using MortierFu.Shared;
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
        private Selectable _firstSelected;

        private InputSystemUIInputModule _previousUiInputModule;
        private PlayerControlContext _previousContext;

        private bool _isActive;

        public void Begin(PlayerManager player, EventSystem eventSystem, InputSystemUIInputModule uiInputModule, Selectable firstSelected)
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

            _previousContext = player.ControlContext;
            _previousUiInputModule = player.PlayerInput.uiInputModule;

            _eventSystem.enabled = true;
            _uiInputModule.enabled = true;

            _eventSystem.SetSelectedGameObject(null);

            _player.PlayerInput.uiInputModule = _uiInputModule;
            _player.SetUnityEventSystemUIActive(true);
            _player.SetControlContext(PlayerControlContext.LobbySettingsOwner);

            if (firstSelected)
            {
                _eventSystem.SetSelectedGameObject(firstSelected.gameObject);
                _firstSelected = firstSelected;
            }
                

            _isActive = true;
        }

        public void Begin()
        {
            Begin(_player, _eventSystem, _uiInputModule, _firstSelected);
        }

        public void End()
        {
            if (!_isActive)
                return;

            if (_eventSystem)
                _eventSystem.SetSelectedGameObject(null);

            if (_player)
            {
                _player.SetUnityEventSystemUIActive(false);
                _player.PlayerInput.uiInputModule = _previousUiInputModule;
                _player.SetControlContext(_previousContext);
            }

            _player = null;
            _eventSystem = null;
            _uiInputModule = null;
            _previousUiInputModule = null;
            _isActive = false;
        }
    }
}