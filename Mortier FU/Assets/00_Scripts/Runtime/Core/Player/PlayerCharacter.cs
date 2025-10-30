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
        
        private StateMachine _stateMachine;
        
        private InputAction _strikeAction;
        private InputAction _toggleAimAction;
        
        public PlayerManager Owner { get; private set; }
        public HealthCharacterComponent Health { get; private set; }
        public ControllerCharacterComponent Controller { get; private set; }
        public MortarCharacterComponent Mortar { get; private set; }
        
        [field: SerializeField, Expandable, ShowIf("ShouldShowStats")]
        public SO_CharacterStats CharacterStats { get; private set; }
        
        public Color PlayerColor => _playerColor;

        private List<IAugment> _augments = new();
        public ReadOnlyCollection<IAugment> Augments;
        
        private StunState _stunState;

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
            
            // Find and cache Input Actions
            FindInputAction("Strike", out _strikeAction);
            FindInputAction("ToggleAim", out _toggleAimAction);
            
            InitStateMachine();
            
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

        private void InitStateMachine()
        {
            _stateMachine = new StateMachine();
            
            // Declare States
            var locomotionState = new LocomotionState(this);
            var aimState = new AimState(this);
            _stunState = new StunState(this);
            var strikeState = new StrikeState(this);
            var deathState = new DeathState(this);
            
            // Define transitions
            At(_stunState, locomotionState, new FuncPredicate(() => !_stunState.IsActive));
            At(strikeState, locomotionState, new FuncPredicate(() => !strikeState.IsFinished));
            At(locomotionState, strikeState, new FuncPredicate(() => _strikeAction.triggered && !strikeState.InCooldown));
            At(locomotionState, aimState, new FuncPredicate(() => _toggleAimAction.IsPressed()));
            At(aimState, locomotionState, new FuncPredicate(() => !_toggleAimAction.IsPressed()));
            At(aimState, strikeState, new FuncPredicate(() => _strikeAction.triggered && !strikeState.InCooldown));

            Any(deathState, new FuncPredicate(() => !Health.IsAlive));
            Any(_stunState, new FuncPredicate(() => _stunState.IsActive && Health.IsAlive));
            
            // Set initial state
            _stateMachine.SetState(locomotionState);
        }

        public void ReceiveStun(float duration)
        {
            _stunState.ReceiveStun(duration);
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
            _stateMachine.Update();
            
            Health.Update();
            Controller.Update();
            Mortar.Update();
        }

        private void FixedUpdate()
        {
            _stateMachine.FixedUpdate();
            
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
        
        private void At(IState from, IState to, IPredicate condition) => _stateMachine.AddTransition(from, to, condition);
        private void Any(IState to, IPredicate condition) => _stateMachine.AddAnyTransition(to, condition);
        
#if UNITY_EDITOR
        // Useful to show only when the stats are initialized per player and prevent thinking we have to assign it in the inspector
        private bool ShouldShowStats => CharacterStats != null;
#endif
    }
}