using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using MortierFu;
using MortierFu.Shared;
using TMPro;

public class PlayerGameplayUI : MonoBehaviour {
    [Header("References")]
    [SerializeField] private PlayerCharacter _character;
    [SerializeField] private Image _healthFillImg;
    [SerializeField] private Image _strikeCdImage;
    [SerializeField] private Image _shootCdImage;
    [SerializeField] private Image _CharacterArrowImage;
    [SerializeField] private TMP_Text _playerIndexText;
    
    private void Update()
    {
        // Reverse progress bar
        float strikeProgress = 1 - _character.GetStrikeCooldownProgress;
        float shootProgress = 1 - _character.Mortar.ShootCooldownProgress;
        
        _strikeCdImage.enabled = strikeProgress < 1f;
        _strikeCdImage.fillAmount = strikeProgress;
        
        _shootCdImage.enabled = shootProgress < 1f;
        _shootCdImage.fillAmount = shootProgress;
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
        
        // Initialize Character Arrow Color and Player Index Text
        GetColorAndIndex().Forget();
    }
    
    private async UniTaskVoid GetColorAndIndex()
    {
        // Wait a frame to ensure all components are initialized
        await UniTask.Yield();
        
        // Change color of the arrow to match the character
        if (_CharacterArrowImage != null && _character != null)
        {
            Color characterColor = _character.Aspect.PlayerColor;
            _CharacterArrowImage.color = characterColor;
        }
        
        // Set player index text
        if (_playerIndexText != null)
        {
            string playerIndexStr = "Player " + (_character.Owner.PlayerIndex + 1).ToString();
            _playerIndexText.text = playerIndexStr;
        }  
    }

    private void OnDisable()
    {
        if(_character == null) return;
        _character.Health.OnHealthChanged -= OnHealthChanged;
    }
}
