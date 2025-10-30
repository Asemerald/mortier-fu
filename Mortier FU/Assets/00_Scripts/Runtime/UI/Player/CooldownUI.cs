using UnityEngine;
using UnityEngine.UI;
using MortierFu;

public class CooldownUI : MonoBehaviour
{
    [SerializeField] private Image _strikeCdImage;
    private Camera _mainCamera;
    private PlayerController _characterController;

    private void UpdateUI()
    {
        _strikeCdImage.fillAmount = (_characterController._strikeCooldownTimer.CurrentTime / _characterController.CharacterStats.StrikeCooldown.Value);
    }

    public void SetController(PlayerController controller)
    {
        _characterController = controller;
    }
    

    private void Update()
    {
        UpdateUI();
    }
}
