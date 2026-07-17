using System;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public sealed class PlayerInputRouter : IDisposable
    {
        private readonly PlayerInput _playerInput;

        private readonly Action<InputAction.CallbackContext> _onPause;
        private readonly Action<InputAction.CallbackContext> _onNavigateUI;
        private readonly Action<InputAction.CallbackContext> _onSubmitUI;
        private readonly Action<InputAction.CallbackContext> _onCancelUI;

        private InputAction _pauseAction;
        private InputAction _unPauseAction;

        private InputAction _navigateUIAction;
        private InputAction _submitUIAction;
        private InputAction _cancelUIAction;

        private bool _callbacksBound;

        public PlayerControlContext ControlContext { get; private set; } = PlayerControlContext.Lobby;

        public PlayerActionPermissions CurrentPermissions { get; private set; } = PlayerActionPermissions.FromContext(PlayerControlContext.Lobby);

        public PlayerInputRouter(PlayerInput playerInput, Action<InputAction.CallbackContext> onPause, Action<InputAction.CallbackContext> onNavigateUI, Action<InputAction.CallbackContext> onSubmitUI, Action<InputAction.CallbackContext> onCancelUI)
        {
            _playerInput = playerInput ?? throw new ArgumentNullException(nameof(playerInput));

            _onPause = onPause ?? throw new ArgumentNullException(nameof(onPause));
            _onNavigateUI = onNavigateUI ?? throw new ArgumentNullException(nameof(onNavigateUI));
            _onSubmitUI = onSubmitUI ?? throw new ArgumentNullException(nameof(onSubmitUI));
            _onCancelUI = onCancelUI ?? throw new ArgumentNullException(nameof(onCancelUI));
        }

        public void SetContext(PlayerControlContext context, PlayerCharacter character = null)
        {
            ControlContext = context;
            CurrentPermissions = PlayerActionPermissions.FromContext(context);

            string targetMap = UsesGameplayActionMap(context)
                ? PlayerInputActionNames.GameplayMap
                : PlayerInputActionNames.UIMap;

            _playerInput.SwitchCurrentActionMap(targetMap);

            InputActionMap globalMap = _playerInput.actions.FindActionMap(PlayerInputActionNames.GlobalMap);
            globalMap?.Enable();

            character?.SetControlContext(context);
        }

        public void ApplyCurrentContextTo(PlayerCharacter character) => character?.SetControlContext(ControlContext);

        public void BindInputCallbacks()
        {
            if (_callbacksBound)
                return;

            _pauseAction = _playerInput.actions.FindAction(PlayerInputActionNames.Pause);
            _unPauseAction = _playerInput.actions.FindAction(PlayerInputActionNames.UnPause);

            _navigateUIAction = _playerInput.actions.FindAction(PlayerInputActionNames.Navigate);
            _submitUIAction = _playerInput.actions.FindAction(PlayerInputActionNames.Submit);
            _cancelUIAction = _playerInput.actions.FindAction(PlayerInputActionNames.Cancel);

            if (_pauseAction is not null)
                _pauseAction.performed += _onPause;

            if (_unPauseAction is not null)
                _unPauseAction.performed += _onPause;

            if (_navigateUIAction is not null)
            {
                _navigateUIAction.performed += _onNavigateUI;
                _navigateUIAction.canceled += _onNavigateUI;
            }

            if (_submitUIAction is not null)
                _submitUIAction.performed += _onSubmitUI;

            if (_cancelUIAction is not null)
                _cancelUIAction.performed += _onCancelUI;

            _callbacksBound = true;
        }

        private void UnbindInputCallbacks()
        {
            if (!_callbacksBound)
                return;

            if (_pauseAction is not null)
                _pauseAction.performed -= _onPause;

            if (_unPauseAction is not null)
                _unPauseAction.performed -= _onPause;

            if (_navigateUIAction is not null)
            {
                _navigateUIAction.performed -= _onNavigateUI;
                _navigateUIAction.canceled -= _onNavigateUI;
            }

            if (_submitUIAction is not null)
                _submitUIAction.performed -= _onSubmitUI;

            if (_cancelUIAction is not null)
                _cancelUIAction.performed -= _onCancelUI;

            _pauseAction = null;
            _unPauseAction = null;

            _navigateUIAction = null;
            _submitUIAction = null;
            _cancelUIAction = null;

            _callbacksBound = false;
        }

        public void Dispose()
        {
            UnbindInputCallbacks();
        }

        private static bool UsesGameplayActionMap(PlayerControlContext context)
        {
            return context is PlayerControlContext.LobbySandbox
                or PlayerControlContext.AugmentRace
                or PlayerControlContext.AugmentRaceBullyClassic
                or PlayerControlContext.AugmentRaceBullyMoveOnly
                or PlayerControlContext.AugmentRaceBullyShootOnly
                or PlayerControlContext.AugmentRaceBullyLocked
                or PlayerControlContext.RoundCountdown
                or PlayerControlContext.RoundGameplay
                or PlayerControlContext.RoundEnded
                or PlayerControlContext.RoundGhost;
        }

    }
}