using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class Character : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DA_CharacterData _characterDataTemplate;
        [SerializeField] private HealthUI _healthUI;
        
        private Color _playerColor;
        
        [field: SerializeField] public DA_CharacterData CharacterData { get; private set; }
        public Health Health { get; private set; }

        void Awake()
        {
            // Create a unique instance of CharacterData for this character
            CharacterData = Instantiate(_characterDataTemplate);
            
            // Initialize the health component based on that Data
            Health = new Health(CharacterData);
        }
        
        private void Start()
        {
            if (_healthUI != null)
            {
                _healthUI.SetHealth(Health);
            }
            
            // TEMPORARY: Choose a random color
            _playerColor = ColorUtils.RandomizedHue();
            
            // Apply it
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