using UnityEngine;

namespace MortierFu
{
    public class Character : MonoBehaviour
    {
        [SerializeField] private HealthUI _healthUI;
        public Health Health { get; } = new(100.0f);
    
        private void Awake()
        {
            if (_healthUI != null)
            {
                _healthUI.SetHealth(Health);
            }
        }
    }
   
}