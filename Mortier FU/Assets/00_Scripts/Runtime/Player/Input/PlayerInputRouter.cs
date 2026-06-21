using System;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public sealed class PlayerInputRouter : IDisposable
    {
        private readonly PlayerInput _playerInput;
        private readonly Action<InputAction.CallbackContext> _onPause;
        private readonly Action<InputAction.CallbackContext> _onCancelUI;

        private InputAction _pauseAction;
        private InputAction _unPauseAction;
        private InputAction _cancelUIAction;

        private bool _gameplayCallbacksBound;

        public PlayerControlContext ControlContext { get; private set; } = PlayerControlContext.Lobby;

        public PlayerActionPermissions CurrentPermissions { get; private set; } =
            PlayerActionPermissions.FromContext(PlayerControlContext.Lobby);

        public PlayerInputRouter(
            PlayerInput playerInput,
            Action<InputAction.CallbackContext> onPause,
            Action<InputAction.CallbackContext> onCancelUI)
        {
            _playerInput = playerInput ?? throw new ArgumentNullException(nameof(playerInput));
            _onPause = onPause ?? throw new ArgumentNullException(nameof(onPause));
            _onCancelUI = onCancelUI ?? throw new ArgumentNullException(nameof(onCancelUI));
        }

        public void SetContext(PlayerControlContext context, PlayerCharacter character = null)
        {
            ControlContext = context;
            CurrentPermissions = PlayerActionPermissions.FromContext(context);

            string targetMap = UsesGameplayActionMap(context)
                ? "Gameplay"
                : "UI";

            _playerInput.SwitchCurrentActionMap(targetMap);

            var globalMap = _playerInput.actions.FindActionMap("Global", false);
            globalMap?.Enable();

            character?.SetControlContext(context);
        }

        public void ApplyCurrentContextTo(PlayerCharacter character)
        {
            character?.SetControlContext(ControlContext);
        }

        public void BindGameplayInputCallbacks()
        {
            if (_gameplayCallbacksBound)
                return;

            _pauseAction = _playerInput.actions.FindAction("Pause", false);
            _unPauseAction = _playerInput.actions.FindAction("UnPause", false);
            _cancelUIAction = _playerInput.actions.FindAction("Cancel", false);

            if (_pauseAction != null)
                _pauseAction.performed += _onPause;

            if (_unPauseAction != null)
                _unPauseAction.performed += _onPause;

            if (_cancelUIAction != null)
                _cancelUIAction.performed += _onCancelUI;

            _gameplayCallbacksBound = true;
        }

        public void UnbindGameplayInputCallbacks()
        {
            if (!_gameplayCallbacksBound)
                return;

            if (_pauseAction != null)
                _pauseAction.performed -= _onPause;

            if (_unPauseAction != null)
                _unPauseAction.performed -= _onPause;

            if (_cancelUIAction != null)
                _cancelUIAction.performed -= _onCancelUI;

            _pauseAction = null;
            _unPauseAction = null;
            _cancelUIAction = null;

            _gameplayCallbacksBound = false;
        }

        public void Dispose()
        {
            UnbindGameplayInputCallbacks();
        }

        private static bool UsesGameplayActionMap(PlayerControlContext context)
        {
            return context is PlayerControlContext.AugmentRace
                or PlayerControlContext.RoundCountdown
                or PlayerControlContext.RoundGameplay
                or PlayerControlContext.RoundEnded;
        }
    }
}