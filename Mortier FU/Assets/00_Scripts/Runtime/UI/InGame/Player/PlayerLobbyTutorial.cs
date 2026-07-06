using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MortierFu
{

    public class PlayerLobbyTutorial
    {
        private List<SO_Tutorial> _tutorialBinding;

        private Image _tutorialSlot;
        
        private TextMeshProUGUI _tutorialText;

        private InputActionReference _currentInputToPress;
        
        private InputActionReference _aimToggleInputReference;

        private PlayerManager _playerCharacter;
        
        private int index = 0;
        
        
        
        
        CountdownTimer timer;

        public PlayerLobbyTutorial(List<SO_Tutorial> pTutorialBinding, PlayerManager pCharacter)
        {
            if (pTutorialBinding.Count == 0)
                return;
            
            timer = new CountdownTimer(0.3f);
            timer.OnTimerStop += InitTuto;
            timer.Start();
            
            _tutorialBinding = pTutorialBinding;
            _tutorialSlot = pCharacter.Character.TutorialImage;
            _tutorialText = pCharacter.Character.TutorialText;
            _playerCharacter = pCharacter;
            _tutorialSlot.gameObject.SetActive(true);
            _tutorialText.gameObject.SetActive(true);
        }

        private void InitTuto()
        {
            timer.OnTimerStop -= InitTuto;
            //save the active aim input in case the player skip it unintentionally
            _aimToggleInputReference = _tutorialBinding[1].inputAction;
            
            _currentInputToPress = _tutorialBinding[index].inputAction;
            _tutorialSlot.sprite = _tutorialBinding[index].image;
            _tutorialText.text = _tutorialBinding[index].explanationText;

            _playerCharacter.PlayerInput.currentActionMap.actionTriggered += UpdateStepTuto;
            
        }

        private void UpdateStepTuto(InputAction.CallbackContext ctx)
        {
            HoldCheck(ctx);
            
            if (ctx.action.name != _currentInputToPress.action.name)
                return;
            
            if (index != _tutorialBinding.Count - 1 )
            {
                
                index++;
                UpdateVisual();
                
            }
            else
            {
                _playerCharacter.PlayerInput.currentActionMap.actionTriggered -= UpdateStepTuto;
                _tutorialSlot.gameObject.SetActive(false);
                _tutorialText.gameObject.SetActive(false);
            }
            
        }

        private void UpdateVisual()
        {
            _currentInputToPress = _tutorialBinding[index].inputAction;
            _tutorialSlot.sprite = _tutorialBinding[index].image;
            _tutorialText.text = _tutorialBinding[index].explanationText;
            
            Debug.Log(index + " " +_tutorialBinding[index].inputAction);
        }

        private void HoldCheck(InputAction.CallbackContext ctx)
        {
            if (ctx.action.name == _aimToggleInputReference.action.name && ctx.canceled && index !=0)
            {
                index = 1;
                UpdateVisual();
            }
        }

        public void Disconnect()
        {
            if (_tutorialBinding == null)
                return;
            
            timer.OnTimerStop -= InitTuto;
            _playerCharacter.PlayerInput.currentActionMap.actionTriggered -= UpdateStepTuto;
            _tutorialSlot.gameObject.SetActive(false);
            _tutorialText.gameObject.SetActive(false);
        }
    }
}

