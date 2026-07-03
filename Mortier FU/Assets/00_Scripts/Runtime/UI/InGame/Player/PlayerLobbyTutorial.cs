using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MortierFu
{

    public class PlayerLobbyTutorial
    {
        private List<SO_Tutorial> _tutorialBinding;

        private Image _tutorialSlot;

        private InputActionReference _currentInputToPress;

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
            _playerCharacter = pCharacter;
            _tutorialSlot.gameObject.SetActive(true);
        }

        private void InitTuto()
        {
            timer.OnTimerStop -= InitTuto;

            _currentInputToPress = _tutorialBinding[index].inputAction;
            _tutorialSlot.sprite = _tutorialBinding[index].image;

            _playerCharacter.PlayerInput.currentActionMap.actionTriggered += UpdateStepTuto;
        }

        private void UpdateStepTuto(InputAction.CallbackContext ctx)
        {
            if (ctx.action.name != _currentInputToPress.action.name)
                return;
            
            if (index != _tutorialBinding.Count - 1)
            {
               
                index++;
                _currentInputToPress = _tutorialBinding[index].inputAction;
                _tutorialSlot.sprite = _tutorialBinding[index].image;
            }
            else
            {
                _playerCharacter.PlayerInput.currentActionMap.actionTriggered -= UpdateStepTuto;
                _tutorialSlot.gameObject.SetActive(false);
            }
        }

        public void Disconnect()
        {
            timer.OnTimerStop -= InitTuto;
            _playerCharacter.PlayerInput.currentActionMap.actionTriggered -= UpdateStepTuto;
            _tutorialSlot.gameObject.SetActive(false);
        }
    }
}

