using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;

namespace MortierFu
{
    public sealed class PlayerLobbyTutorial
    {
        private readonly List<SO_Tutorial> _steps;
        private readonly PlayerManager _player;
        private readonly PlayerCharacter _character;
        private readonly PlayerTutorialView _view;
        private readonly PlayerGameplayUI _gameplayUI;
        private readonly CancellationTokenSource _cancellation = new();
        private readonly HashSet<PlayerLobbyTutorialAction> _performedActions = new();

        private int _index;
        private bool _isAimHeld;
        private bool _isRunning;
        private bool _isVisible;
        private bool _isDisposed;

        public PlayerLobbyTutorial(List<SO_Tutorial> steps, PlayerManager player)
        {
            _steps = steps;
            _player = player;
            _character = player ? player.Character : null;

            if (_steps == null || _steps.Count == 0 || !_player || !_character)
            {
                Logs.LogWarning("[PlayerLobbyTutorial] Cannot start tutorial because references or steps are missing.");
                return;
            }

            _view = _character.TutorialView;
            _gameplayUI = _character.GameplayUI;

            if (!_view)
            {
                Logs.LogWarning("[PlayerLobbyTutorial] TutorialView reference is missing on PlayerCharacter.");
                return;
            }

            _view.HideInstant();

            RunAsync(_cancellation.Token).Forget();
        }

        private async UniTaskVoid RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                await WaitUntilPlayerCanRunTutorialAsync(cancellationToken);

                if (_isDisposed || !_character)
                    return;

                _character.OnTutorialActionPerformed -= HandleTutorialAction;
                _character.OnTutorialActionPerformed += HandleTutorialAction;

                _isRunning = true;

                await WaitUntilHudIntroReadyAsync(cancellationToken);

                if (_isDisposed)
                    return;

                SkipAlreadyPerformedSteps();

                SO_Tutorial firstStep = GetCurrentStep();

                if (!firstStep)
                {
                    Complete();
                    return;
                }

                bool isKeyboard = _player.IsKeyboardAndMouseControlScheme();

                await _view.ShowStepAsync(firstStep, isKeyboard, cancellationToken);

                _isVisible = true;
            }
            catch (OperationCanceledException)
            { }
            catch (Exception e)
            {
                Logs.LogError($"[PlayerLobbyTutorial] Failed to start tutorial: {e.Message}");
                Disconnect();
            }
        }

        private async UniTask WaitUntilPlayerCanRunTutorialAsync(CancellationToken cancellationToken)
        {
            while (!_isDisposed)
            {
                if (_character && _character.CanProgressLobbyTutorial)
                    return;

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        private async UniTask WaitUntilHudIntroReadyAsync(CancellationToken cancellationToken)
        {
            if (!_gameplayUI)
            {
                Logs.LogWarning("[PlayerLobbyTutorial] GameplayUI reference is missing. Tutorial will start without waiting for HUD intro.");
                return;
            }

            if (_gameplayUI.IsIntroReady)
                return;

            bool introReady = false;

            void HandleIntroReady(bool ready)
            {
                introReady = true;
            }

            _gameplayUI.OnIntroReady += HandleIntroReady;

            try
            {
                while (!_isDisposed && !introReady && !_gameplayUI.IsIntroReady)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            finally
            {
                if (_gameplayUI)
                    _gameplayUI.OnIntroReady -= HandleIntroReady;
            }
        }

        private void HandleTutorialAction(PlayerLobbyTutorialAction action)
        {
            if (!_isRunning || _isDisposed)
                return;

            if (!_character || !_character.CanProgressLobbyTutorial)
                return;

            UpdateRuntimeFlags(action);

            if (action == PlayerLobbyTutorialAction.AimReleased)
            {
                HandleAimReleased();
                return;
            }

            _performedActions.Add(action);

            SO_Tutorial currentStep = GetCurrentStep();

            if (!currentStep)
            {
                Complete();
                return;
            }

            if (!CanValidateCurrentStep(currentStep, action))
                return;

            Advance();
        }

        private void UpdateRuntimeFlags(PlayerLobbyTutorialAction action)
        {
            if (action == PlayerLobbyTutorialAction.Aim)
            {
                _isAimHeld = true;
                return;
            }

            if (action == PlayerLobbyTutorialAction.AimReleased)
                _isAimHeld = false;
        }

        private bool CanValidateCurrentStep(SO_Tutorial step, PlayerLobbyTutorialAction action)
        {
            if (step.RequiredAction != action)
                return false;

            return !step.RequiresAimHeld || _isAimHeld;
        }

        private void HandleAimReleased()
        {
            SO_Tutorial currentStep = GetCurrentStep();

            if (!currentStep)
                return;

            if (!currentStep.ReturnToAimStepWhenAimReleased)
                return;

            ResetToAction(PlayerLobbyTutorialAction.Aim);
        }

        private void Advance()
        {
            _index++;

            SkipAlreadyPerformedSteps();

            if (_index >= _steps.Count)
            {
                Complete();
                return;
            }

            ApplyCurrentStep();
        }

        private void SkipAlreadyPerformedSteps()
        {
            while (_index < _steps.Count)
            {
                SO_Tutorial step = _steps[_index];

                if (!step)
                {
                    _index++;
                    continue;
                }

                if (!step.SkipIfAlreadyPerformed)
                    return;

                if (!_performedActions.Contains(step.RequiredAction))
                    return;

                _index++;
            }
        }

        private void ResetToAction(PlayerLobbyTutorialAction action)
        {
            int targetIndex = FindStepIndex(action);

            if (targetIndex < 0)
                return;

            _index = targetIndex;

            RemovePerformedActionsFromIndex(_index);

            ApplyCurrentStep();
        }

        private int FindStepIndex(PlayerLobbyTutorialAction action)
        {
            for (int i = 0; i < _steps.Count; i++)
            {
                SO_Tutorial step = _steps[i];

                if (!step)
                    continue;

                if (step.RequiredAction == action)
                    return i;
            }

            return -1;
        }

        private void RemovePerformedActionsFromIndex(int startIndex)
        {
            for (int i = startIndex; i < _steps.Count; i++)
            {
                SO_Tutorial step = _steps[i];

                if (!step)
                    continue;

                _performedActions.Remove(step.RequiredAction);
            }
        }

        private void ApplyCurrentStep()
        {
            if (!_isVisible)
                return;

            SO_Tutorial step = GetCurrentStep();

            if (!step)
            {
                Complete();
                return;
            }

            bool isKeyboard = _player.IsKeyboardAndMouseControlScheme();
            _view.ApplyStep(step, isKeyboard);
        }

        private SO_Tutorial GetCurrentStep()
        {
            if (_index < 0 || _index >= _steps.Count)
                return null;

            return _steps[_index];
        }

        private void Complete()
        {
            PlayerLobbyTutorialSession.MarkCompleted(_player.PlayerIndex);
            Disconnect();
        }

        public void Disconnect()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _isRunning = false;
            _isVisible = false;

            _cancellation.Cancel();

            if (_character)
                _character.OnTutorialActionPerformed -= HandleTutorialAction;

            if (_view)
                _view.HideInstant();

            _cancellation.Dispose();
        }
    }
}