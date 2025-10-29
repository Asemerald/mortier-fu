using UnityEngine;
using UnityEngine.UI;
using MortierFu;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private Canvas _healthCanvas;
    [SerializeField] private Image _healthImage;
    
    private Camera _mainCamera;
    
    private HealthCharacterComponent _healthCharacterComponent;

    private void OnEnable()
    {
        _mainCamera = Camera.main;
        
        if (_healthCharacterComponent != null)
        {
            _healthCharacterComponent.OnHealthChanged += OnHealthChanged;
        }
        
        UpdateUI();
    }

    private void OnDisable()
    {
        if (_healthCharacterComponent != null)
        {
            _healthCharacterComponent.OnHealthChanged -= OnHealthChanged;
        }
    }

    private void OnHealthChanged(float amount)
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_healthImage != null && _healthCharacterComponent != null)
        {
            _healthImage.fillAmount = _healthCharacterComponent.HealthRatio;
        }
    }

    public void SetHealth(HealthCharacterComponent newHealthCharacterComponent)
    {
        if (_healthCharacterComponent != null)
        {
            _healthCharacterComponent.OnHealthChanged -= OnHealthChanged;
        }
        _healthCharacterComponent = newHealthCharacterComponent;
        
        if (_healthCharacterComponent != null)
        {
            _healthCharacterComponent.OnHealthChanged += OnHealthChanged;
        }
        UpdateUI();
    }

    private void Update()
    {
        if (_healthCanvas != null && _mainCamera != null)
        {
            _healthCanvas.transform.rotation = Quaternion.LookRotation(_mainCamera.transform.forward, Vector3.up);
        }
    }
}
