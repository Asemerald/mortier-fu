using UnityEngine;
using UnityEngine.UI;
using MortierFu;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private Image healthImage;
    
    private Health health;

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnHealthChanged += OnHealthChanged;
        }
        
        UpdateUI();
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnHealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(float amount)
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (healthImage != null && health != null)
        {
            healthImage.fillAmount = health.HealthRatio;
        }
    }

    public void SetHealth(Health newHealth)
    {
        if (health != null)
        {
            health.OnHealthChanged -= OnHealthChanged;
        }
        health = newHealth;
        
        if (health != null)
        {
            health.OnHealthChanged += OnHealthChanged;
        }
        UpdateUI();
    }
}
