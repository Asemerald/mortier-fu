using System;
using UnityEngine;
using UnityEngine.UI;
using MortierFu;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private Canvas _healthCanvas;
    [SerializeField] private Image _healthImage;
    
    private Camera _mainCamera;
    
    private Health _health;

    private void OnEnable()
    {
        _mainCamera = Camera.main;
        
        if (_health != null)
        {
            _health.OnHealthChanged += OnHealthChanged;
        }
        
        UpdateUI();
    }

    private void OnDisable()
    {
        if (_health != null)
        {
            _health.OnHealthChanged -= OnHealthChanged;
        }
    }

    private void OnHealthChanged(float amount)
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_healthImage != null && _health != null)
        {
            _healthImage.fillAmount = _health.HealthRatio;
        }
    }

    public void SetHealth(Health newHealth)
    {
        if (_health != null)
        {
            _health.OnHealthChanged -= OnHealthChanged;
        }
        _health = newHealth;
        
        if (_health != null)
        {
            _health.OnHealthChanged += OnHealthChanged;
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
