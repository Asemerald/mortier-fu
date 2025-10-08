using UnityEngine;

namespace MortierFu
{
    public class Character : MonoBehaviour
    {
        [SerializeField] private HealthUI _healthUI;
        public Health Health { get; } = new(100.0f);

        private Color _playerColor;
    
        private void Start()
        {
            if (_healthUI != null)
            {
                _healthUI.SetHealth(Health);
            }
            
            // TEMPORARY: Choose a random color
            _playerColor = Random.ColorHSV();
            ;
            if (TryGetComponent(out Renderer rend))
            {
                rend.material.color = _playerColor;
            }

            if (TryGetComponent(out Mortar mortar))
            {
                mortar.AimWidget.GetComponent<Renderer>().material.color = _playerColor;
            }
        }
    }
   
}