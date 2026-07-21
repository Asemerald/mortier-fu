using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using PrimeTween;

namespace MortierFu
{
    public class PlayerLobbyTutorial
    {
        private readonly List<SO_Tutorial> _tutorialBinding;
        private readonly Image _tutorialSlot;
        private readonly TextMeshProUGUI _tutorialText;
        private readonly PlayerManager _playerCharacter;
        private readonly CountdownTimer _timer;

        private List<InputActionReference> _currentInputToPress;
        private List<InputActionReference> _aimToggleInputReference;
        private InputActionMap _currentInputMap;

        private int _index;
        private bool _isSubscribed;
        private const float timeInputDisable = 0.3f;
        private const float appearTweenDuration = 0.5f;
        private const float _startFadeDelay = 2f;

        private readonly Transform _tutorialContainer;

        public PlayerLobbyTutorial(List<SO_Tutorial> pTutorialBinding, PlayerManager pCharacter)
        {
            if (pTutorialBinding == null || pTutorialBinding.Count == 0)
                return;

            _tutorialBinding = pTutorialBinding;
            _tutorialSlot = pCharacter.Character.TutorialImage;
            _tutorialText = pCharacter.Character.TutorialText;
            _tutorialContainer = pCharacter.Character.TutorialContainer;
            _playerCharacter = pCharacter;

            SetVisible(true);

            _timer = new CountdownTimer(timeInputDisable);
            _timer.OnTimerStop += InitTuto;
            _timer.Start();
        }

        private void InitTuto()
        {
            _timer.OnTimerStop -= InitTuto;

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
                if (!IsActionInList(ctx.action.name, _currentInputToPress))
                {
                    return;
                }
                
                
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

        private bool IsActionInList(string actionName, List<InputActionReference> list)
        {
            if (list == null)
                return false;

            if (IsActionInReferenceList(actionName, list))
                return true;

            foreach (SO_Tutorial inputs in _tutorialBinding)
            {
                foreach (var reference in inputs.inputAction)
                {
                    if (reference != null && reference.action.name == actionName)
                    {
                        List<SO_Tutorial> toRemove = CheckConnectedGroup(inputs);
                        if (toRemove == null)
                            return false;
                        foreach (SO_Tutorial tutorial in toRemove)
                        {
                            _tutorialBinding.Remove(tutorial);
                        }

                        return false;
                    }
                }
            }

            return false;
        }

        private List<SO_Tutorial> CheckConnectedGroup(SO_Tutorial tutorial)
        {
            List<SO_Tutorial> result = new List<SO_Tutorial>();
            int indexInList = _tutorialBinding.IndexOf(tutorial);
            
            bool hasNext = indexInList + 1 < _tutorialBinding.Count;
            if ((hasNext && _tutorialBinding[indexInList + 1].connectedToActionBefore) || indexInList < _index)
            {
                return null;
            }

            if (tutorial.connectedToActionBefore)
            {
                for (int i = indexInList; i >= 0; i--)
                {
                    result.Add(_tutorialBinding[i]);
                    if (!_tutorialBinding[i].connectedToActionBefore || i == 0)
                        return result;
                }
            }
            else
            {
                result.Add(tutorial);
            }

            return result;
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

            if (IsActionInList(ctx.action.name, _aimToggleInputReference))
            {
                if (!_tutorialBinding[_index].connectedToActionBefore)
                    return;

                for (int i = _index -1; i >= 0; i--)
                {
                    if (!_tutorialBinding[i].connectedToActionBefore)
                    {
                        _index = i;
                        UpdateVisual();
                        return;
                    }
                }
            }
        }

        private bool IsActionInReferenceList(string actionName, List<InputActionReference> list)
        {
            if (list == null)
                return false;

            foreach (var reference in list)
            {
                if (reference != null && reference.action.name == actionName)
                    return true;
            }

            return false;
        }

        private void SetVisible(bool visible)
        {
            if (!_tutorialSlot || !_tutorialContainer)
            {
                Debug.LogError($"Reference manquante ! Slot: {_tutorialSlot}, Container: {_tutorialContainer}");
                return;
            }

            if (visible)
            {
                _tutorialContainer.gameObject.SetActive(true);
                _tutorialSlot.gameObject.SetActive(true);
                _tutorialText.gameObject.SetActive(true);

                Debug.Log($"Container actif dans la hiérarchie ? {_tutorialContainer.gameObject.activeInHierarchy}");

                ShowPlayerTuto().Forget();
            }
            else
            {
                _tutorialContainer.gameObject.SetActive(false);
                _tutorialSlot.gameObject.SetActive(false);
                _tutorialText.gameObject.SetActive(false);
            }
        }

        private async UniTask ShowPlayerTuto()
        {
            await UniTask.Yield();

            await UniTask.Delay(TimeSpan.FromSeconds(_startFadeDelay));

            _tutorialContainer.localScale = Vector3.zero;
            Tween.Scale(_tutorialContainer, Vector3.one, appearTweenDuration)
                .OnComplete(() => Debug.Log("Tween terminé, scale final : " + _tutorialContainer.localScale));
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