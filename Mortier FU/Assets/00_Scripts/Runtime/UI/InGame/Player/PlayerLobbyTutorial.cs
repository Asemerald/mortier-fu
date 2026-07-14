using System.Collections.Generic;
using MortierFu.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MortierFu
{
    public class PlayerLobbyTutorial
    {
        private readonly List<SO_Tutorial> _tutorialBinding;
        private readonly Image _tutorialSlot;
        private readonly TextMeshProUGUI _tutorialText;
        private readonly PlayerManager _playerCharacter;
        private readonly CountdownTimer _timer;

        private InputActionReference _currentInputToPress;
        private InputActionReference _aimToggleInputReference;
        private InputActionMap _currentInputMap;

        private int _index;
        private bool _isSubscribed;
        private const float timeInputDisable = 0.3f;

        public PlayerLobbyTutorial(List<SO_Tutorial> pTutorialBinding, PlayerManager pCharacter)
        {
            if (pTutorialBinding == null || pTutorialBinding.Count == 0)
                return;

            _tutorialBinding = pTutorialBinding;
            _tutorialSlot = pCharacter.Character.TutorialImage;
            _tutorialText = pCharacter.Character.TutorialText;
            _playerCharacter = pCharacter;

            SetVisible(true);

            _timer = new CountdownTimer(timeInputDisable);
            _timer.OnTimerStop += InitTuto;
            _timer.Start();
        }

        private void InitTuto()
        {
            _timer.OnTimerStop -= InitTuto;

            // save the active aim input in case the player skips it unintentionally
            if (_tutorialBinding.Count > 1)
                _aimToggleInputReference = _tutorialBinding[1].inputAction;

            UpdateVisual();

            _currentInputMap = _playerCharacter.PlayerInput.currentActionMap;
            _currentInputMap.actionTriggered += UpdateStepTuto;
            _isSubscribed = true;
        }

        private void UpdateStepTuto(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
            {
                if (ctx.action.name != _currentInputToPress.action.name)
                    return;

                if (_index != _tutorialBinding.Count - 1)
                {
                    _index++;
                    UpdateVisual();
                }
                else
                {
                    Unsubscribe();
                    SetVisible(false);
                }
            }
            else if (ctx.canceled)
            {
                HoldCheck(ctx);
            }
        }

        private void UpdateVisual()
        {
            bool isKbm = _playerCharacter.IsKeyboardAndMouseControlScheme();

            _currentInputToPress = _tutorialBinding[_index].inputAction;
            _tutorialSlot.sprite = _tutorialBinding[_index].GetSpriteByInput(isKbm);
            _tutorialSlot.rectTransform.sizeDelta = _tutorialBinding[_index].GetSizeByInput(isKbm);
            _tutorialText.text = _tutorialBinding[_index].explanationText;
        }

        private void HoldCheck(InputAction.CallbackContext ctx)
        {
            if (_aimToggleInputReference == null)
                return;

            if (ctx.action.name == _aimToggleInputReference.action.name && _index != 0)
            {
                _index = 1;
                UpdateVisual();
            }
        }

        private void SetVisible(bool visible)
        {
            if(!_tutorialSlot) return;
            
            _tutorialSlot.gameObject.SetActive(visible);
            _tutorialText.gameObject.SetActive(visible);
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
                return;

            _currentInputMap.actionTriggered -= UpdateStepTuto;
            _isSubscribed = false;
        }

        public void Disconnect()
        {
            if (_tutorialBinding == null)
                return;

            _timer.OnTimerStop -= InitTuto;
            Unsubscribe();
            SetVisible(false);
        }
    }
}