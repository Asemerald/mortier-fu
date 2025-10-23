using System.Collections.Generic;
using System.Collections.ObjectModel;
using MortierFu.Shared;
using NaughtyAttributes;
using UnityEngine;

namespace MortierFu
{
    public class Character : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SO_CharacterStats _characterStatsTemplate;
        [SerializeField] private HealthUI _healthUI;
        
        private Color _playerColor;
        
        [field: SerializeField, Expandable, ShowIf("ShouldShowStats")]
        public SO_CharacterStats CharacterStats { get; private set; }
        public Health Health { get; private set; }
        public Color PlayerColor => _playerColor;

        private List<IAugment> _augments = new();
        public ReadOnlyCollection<IAugment> Augments;

#if UNITY_EDITOR
        // Useful to show only when the stats are initialized per player and prevent thinking we have to assign it in the inspector
        private bool ShouldShowStats => CharacterStats != null;
#endif
        
        void Awake()
        {
            _augments = new List<IAugment>();
            Augments = _augments.AsReadOnly();
            
            // Create a unique instance of CharacterData for this character
            CharacterStats = Instantiate(_characterStatsTemplate);
            
            // Initialize the health component based on that Data
            Health = new Health(CharacterStats);
            
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

        public void AddAugment(DA_Augment augmentData)
        {
            var augmentInstance = AugmentFactory.Create(augmentData, this);
            augmentInstance.Initialize();
            _augments.Add(augmentInstance);
        }

        // Could also implement a RemoveAugment method if needed
        public void ClearAugments()
        {
            _augments.Clear();
        }

        public void ResetCharacter()
        {
            Health.ResetHealth();
        }
    }
}