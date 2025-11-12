using UnityEngine;
using UnityEngine.UI;
using MortierFu;
using MortierFu.Shared;

public class PlayerGameplayUI : MonoBehaviour {
    [Header("References")]
    [SerializeField] private PlayerCharacter _character;
    [SerializeField] private Image _healthFillImg;
    [SerializeField] private Image _strikeCdImage;
    
    private void Update()
    {
        // Reverse progress bar
        float progress = 1 - _character.GetStrikeCooldownProgress;
        _strikeCdImage.enabled = progress < 1f;
        _strikeCdImage.fillAmount = progress;
    }
    
    private void OnHealthChanged(float amount)
    { 
        _healthFillImg.fillAmount = _character.Health.HealthRatio;
    }
    
    private void OnEnable() {
        if (_character == null) {
            enabled = false;
            Logs.LogWarning("Missing character reference on PlayerGameplayUI. Falling asleep.", this);
            return;
        };
        
        _character.Health.OnHealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        if(_character == null) return;
        _character.Health.OnHealthChanged -= OnHealthChanged;
    }
}
