using System.Collections.Generic;
using System.Collections.ObjectModel;
using MortierFu.Shared;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public class PlayerCharacter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SO_CharacterStats _characterStatsTemplate;
        [Space]
        [SerializeField] private AimWidget _aimWidgetPrefab;
        [SerializeField] private Transform _firePoint;
        [SerializeField] private HealthUI _healthUI;
        private Color _playerColor; // TODO: Make it cleaner
        
        public PlayerManager Owner { get; private set; }
        public HealthCharacterComponent Health { get; private set; }
        public ControllerCharacterComponent Controller { get; private set; }
        public MortarCharacterComponent Mortar { get; private set; }
        
        [field: SerializeField, Expandable, ShowIf("ShouldShowStats")]
        public SO_CharacterStats CharacterStats { get; private set; }
        
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
            // Create character components
            Health = new HealthCharacterComponent(this);
            Controller = new ControllerCharacterComponent(this);
            Mortar = new MortarCharacterComponent(this, _aimWidgetPrefab, _firePoint);

            // Create a unique instance of CharacterData for this character
            CharacterStats = Instantiate(_characterStatsTemplate);
            
            // Assign a random color to the player
            // TODO: Make a better color assignment system - TEMPORARY
            _playerColor = ColorUtils.RandomizedHue();
            if (TryGetComponent(out Renderer rend))
            {
                rend.material.color = _playerColor;
            }
            
            // Handle augments
            _augments = new List<IAugment>();
            Augments = _augments.AsReadOnly();
            
            // TODO: Should not be this way around. Inversion of control
            if (_healthUI != null)
            {
                _healthUI.SetHealth(Health);
            }
        }

        void Start()
        {
            // Initialize character components
            Health.Initialize();
            Controller.Initialize();
            Mortar.Initialize();
        }
        
        public void Reset()
        {
            Health.Reset();
            Controller.Reset();
            Mortar.Reset();
            gameObject.SetActive(true);
        }
        
        void OnDestroy() {
            Health.Dispose();
            Controller.Dispose();
            Mortar.Dispose();
        }

        public void FindInputAction(string actionName, out InputAction action)
        {
#if UNITY_EDITOR
            bool isEditor = true;
#else
            bool isEditor = false;
#endif
            action = PlayerInput.actions.FindAction(actionName, isEditor);
            if (action == null)
            {
                Logs.LogError($"[PlayerCharacter]: Input Action '{actionName}' not found in PlayerInput actions.");
            }
        }
        
        #region Augments
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
        #endregion

        #region Propagate Unity messages to character components
        private void Update()
        {
            Health.Update();
            Controller.Update();
            Mortar.Update();
        }

        private void FixedUpdate()
        {
            Health.FixedUpdate();
            Controller.FixedUpdate();
            Mortar.FixedUpdate();
        }

        private void OnDrawGizmos()
        {
            Health?.OnDrawGizmos();
            Controller?.OnDrawGizmos();
            Mortar?.OnDrawGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            Health?.OnDrawGizmosSelected();
            Controller?.OnDrawGizmosSelected();
            Mortar?.OnDrawGizmosSelected();
        }
        #endregion
        
#if UNITY_EDITOR
        // Useful to show only when the stats are initialized per player and prevent thinking we have to assign it in the inspector
        private bool ShouldShowStats => CharacterStats != null;
#endif
    }
}