using MortierFu.Shared;
using NaughtyAttributes;
using UnityEngine;

namespace MortierFu
{
    public class Character : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DA_CharacterData _characterDataTemplate;
        [SerializeField] private HealthUI _healthUI;
        
        private Color _playerColor;
        
        [field: SerializeField, Expandable]
        public DA_CharacterData CharacterData { get; private set; }
        public Health Health { get; private set; }
        public Color PlayerColor => _playerColor;

        void Awake()
        {
            // Create a unique instance of CharacterData for this character
            CharacterData = Instantiate(_characterDataTemplate);
            
            // Initialize the health component based on that Data
            Health = new Health(CharacterData);
            
            // TEMPORARY: Choose a random color
            _playerColor = ColorUtils.RandomizedHue();
        }
        
        private void Start()
        {
            if (_healthUI != null)
            {
                _healthUI.SetHealth(Health);
            }
            
            // Apply it
            if (TryGetComponent(out Renderer rend))
            {
                rend.material.color = _playerColor;
            }
        }
    }
   
}