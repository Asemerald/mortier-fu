using UnityEngine;
using MortierFu;
public class Character : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 100f;
    
    [SerializeField] private HealthUI _healthUI;
    
    private Health _health;
    private void Awake()
    {
        _health = new Health(_maxHealth);

        if (_healthUI != null)
        {
            _healthUI.SetHealth(_health);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            _health.TakeDamage(20);
        }
    }
}
