using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MortierFu
{
    public sealed class PlayerLobbyTutorial
    {
        private const float k_appearDuration = 0.25f;

        private readonly List<SO_Tutorial> _steps;
        private readonly PlayerManager _player;
        private readonly PlayerCharacter _character;
        private readonly Transform _container;
        private readonly Image _image;
        private readonly TextMeshProUGUI _text;

        private int _index;
        private bool _isRunning;
        private bool _isDisposed;
        private Tween _scaleTween;

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

            _container = _character.TutorialContainer;
            _image = _character.TutorialImage;
            _text = _character.TutorialText;

            HideInstant();

            RunAsync().Forget();
        }

        private async UniTaskVoid RunAsync()
        {
            try
            {
                await WaitUntilPlayerCanRunTutorialAsync();
                await WaitUntilHudIntroReadyAsync();

                if (_isDisposed)
                    return;

                ApplyCurrentStep();

                if (!_container)
                {
                    Logs.LogWarning("[PlayerLobbyTutorial] Tutorial container is missing.");
                    return;
                }

                _container.gameObject.SetActive(true);
                _container.localScale = Vector3.zero;

                _scaleTween = Tween.Scale(
                    _container,
                    Vector3.one,
                    k_appearDuration,
                    Ease.OutBack
                );

                await WaitForTweenAsync(_scaleTween);

                if (_isDisposed || !_character)
                    return;

                _character.OnLobbyTutorialAction -= HandleTutorialAction;
                _character.OnLobbyTutorialAction += HandleTutorialAction;

                _isRunning = true;
            }
            catch (Exception e)
            {
                Logs.LogError($"[PlayerLobbyTutorial] Failed to start tutorial: {e.Message}");
                Disconnect();
            }
        }

        private async UniTask WaitUntilPlayerCanRunTutorialAsync()
        {
            while (!_isDisposed)
            {
                if (_character && _character.CanProgressLobbyTutorial)
                    return;

                await UniTask.Yield();
            }
        }
        
        private async UniTask WaitUntilHudIntroReadyAsync()
        {
            while (!_isDisposed)
            {
                if (_character && _character.IsHudIntroReady)
                    return;

                await UniTask.Yield();
            }
        }

        private void HandleTutorialAction(PlayerLobbyTutorialAction action)
        {
            if (!_isRunning || _isDisposed)
                return;

            if (!_character || !_character.CanProgressLobbyTutorial)
                return;

            if (_index < 0 || _index >= _steps.Count)
                return;

            SO_Tutorial currentStep = _steps[_index];

            if (!currentStep || currentStep.RequiredAction != action)
                return;

            _index++;

            if (_index >= _steps.Count)
            {
                Complete();
                return;
            }

            ApplyCurrentStep();
        }

        private void ApplyCurrentStep()
        {
            if (_index < 0 || _index >= _steps.Count)
                return;

            SO_Tutorial step = _steps[_index];

            if (!step)
            {
                Logs.LogWarning($"[PlayerLobbyTutorial] Tutorial step {_index} is missing.");
                return;
            }

            bool isKeyboard = _player.IsKeyboardAndMouseControlScheme();
            Sprite sprite = step.GetSpriteByInput(isKeyboard);

            if (_image)
            {
                _image.sprite = sprite;
                _image.enabled = sprite != null;

                if (sprite)
                    _image.rectTransform.sizeDelta = step.GetSizeByInput(isKeyboard);
                else
                    Logs.LogWarning($"[PlayerLobbyTutorial] Tutorial step '{step.name}' has no sprite for current input.");
            }

            if (_text)
                _text.text = step.ExplanationText;
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

            if (_scaleTween.isAlive)
                _scaleTween.Stop();

            if (_character)
                _character.OnLobbyTutorialAction -= HandleTutorialAction;

            HideInstant();
        }

        private void HideInstant()
        {
            if (_container)
            {
                _container.localScale = Vector3.zero;
                _container.gameObject.SetActive(false);
            }

            if (_image)
            {
                _image.sprite = null;
                _image.enabled = false;
            }

            if (_text)
                _text.text = string.Empty;
        }

        private static async UniTask WaitForTweenAsync(Tween tween)
        {
            while (tween.isAlive)
                await UniTask.Yield();
        }
    }
}