using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MortierFu.Shared;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public class Character : MonoBehaviour // TODO: Rename this to PlayerCharacter for conviniency
    {
        [Header("References")]
        [SerializeField] private SO_CharacterStats _characterStatsTemplate;
        [SerializeField] private HealthUI _healthUI;
        private Color _playerColor; // TODO: Make it cleaner
        
        [field: SerializeField, Expandable, ShowIf("ShouldShowStats")]
        public SO_CharacterStats CharacterStats { get; private set; }
        
        // Associated Components accessors
        public PlayerManager Owner { get; private set; }
        public HealthCharacterComponent HealthCharacterComponent { get; private set; }
        public PlayerController Controller { get; private set; }
        public Mortar Mortar { get; private set; }
        
        public Color PlayerColor => _playerColor;

        private List<IAugment> _augments = new();
        public ReadOnlyCollection<IAugment> Augments;

        public PlayerInput PlayerInput => Owner?.PlayerInput;

        /// <summary>
        /// Tells the character who possesses it.
        /// </summary>
        public void Initialize(PlayerManager owner)
        {
            Owner = owner;
        }
        
        void Awake()
        {
            _augments = new List<IAugment>();
            Augments = _augments.AsReadOnly();
            
            // Create a unique instance of CharacterData for this character
            CharacterStats = Instantiate(_characterStatsTemplate);
            
            // Initialize the character components
            HealthCharacterComponent = new HealthCharacterComponent(this);
            HealthCharacterComponent.Initialize();
            
            // Get other components
            Controller = GetComponent<PlayerController>();
            Mortar = GetComponent<Mortar>();
            
            // TEMPORARY: Choose a random color
            _playerColor = ColorUtils.RandomizedHue();
            
            // TODO: REMOVE THIS
        }
        
        private void Start()
        {
            if (_healthUI != null)
            {
                _healthUI.SetHealth(HealthCharacterComponent);
            }
            
            // Apply it
            if (TryGetComponent(out Renderer rend))
            {
                rend.material.color = _playerColor;
            }
        }
        
        void OnDestroy() {
            HealthCharacterComponent.Dispose();
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

        public void Reset()
        {
            gameObject.SetActive(true);
            HealthCharacterComponent.Reset();
        }
        
#if UNITY_EDITOR
        // Useful to show only when the stats are initialized per player and prevent thinking we have to assign it in the inspector
        private bool ShouldShowStats => CharacterStats != null;
#endif
    }
}