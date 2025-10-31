using System;
using UnityEngine;
using UnityEngine.UI;
using MortierFu;
using MortierFu.Shared;

public class CooldownUI : MonoBehaviour
{
    [SerializeField] private Image _strikeCdImage;
    private Camera _mainCamera;
    private PlayerCharacter _character;

    private void UpdateUI()
    {
        // Reverse progress bar
        float fillAmount = 1f - _character.GetStrikeCooldownProgress;
        _strikeCdImage.fillAmount = fillAmount >= 1f ? 0f : fillAmount;
    }

    public void SetCharacter(PlayerCharacter controller)
    {
        _character = controller;
    }
    
    private void Update()
    {
        UpdateUI();
    }
}
