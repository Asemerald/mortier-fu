using System.Collections.Generic;
using System.Collections.ObjectModel;
using MortierFu.Shared;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MortierFu
{
    public class PlayerCharacter : MonoBehaviour
    {
        /// <summary>
        /// Set by the game mode when gameplay actions are allowed or not.
        /// </summary>
        public static bool AllowGameplayActions { get; set; }
        
        [Header("Mortar")]
        [SerializeField] private AimWidget _aimWidgetPrefab;
        [SerializeField] private Transform _firePoint;
        
        [Header("Aspect")]
        [Tooltip("Will extract the hue, saturation and value to colorize the player characters.")]
        [SerializeField] private Color _characterColorConfig = Color.white;

        [Header("References")] 
        [SerializeField] private Animator _animator;
        [SerializeField] private SO_CharacterStats _characterStatsTemplate;
        
        private StateMachine _stateMachine;
        
        private InputAction _strikeAction;
        private InputAction _toggleAimAction;
        
        public PlayerManager Owner { get; private set; }
        public HealthCharacterComponent Health { get; private set; }
        public ControllerCharacterComponent Controller { get; private set; }
        public AspectCharacterComponent Aspect { get; private set; }
        public MortarCharacterComponent Mortar { get; private set; }
        
        [field: SerializeField, Expandable, ShowIf("ShouldShowStats")]
        public SO_CharacterStats Stats { get; private set; }

        private List<IAugment> _augments = new();
        public ReadOnlyCollection<IAugment> Augments;

        readonly List<IEffect<PlayerCharacter>> _activeEffects = new();
        
        private LocomotionState _locomotionState;
        private StunState _stunState;
        private StrikeState _strikeState;

        private readonly int _speedHash = Animator.StringToHash("Speed");
        
        public PlayerInput PlayerInput => Owner?.PlayerInput;

        public float GetStrikeCooldownProgress => _strikeState.StrikeCooldownProgress;
        
        /// <summary>
        /// Tells the character who possesses it.
        /// </summary>
        public void Initialize(PlayerManager owner)
        {
            Owner = owner;
        }
        
        void Awake()
        {
            // Extract HSV from the character color config
            Color.RGBToHSV(_characterColorConfig, out float hue, out float saturation, out float value);
            
            // Create character components
            Health = new HealthCharacterComponent(this);
            Controller = new ControllerCharacterComponent(this);
            Aspect = new AspectCharacterComponent(this, hue, saturation, value);
            Mortar = new MortarCharacterComponent(this, _aimWidgetPrefab, _firePoint);
            
            // Create a unique instance of CharacterData for this character
            Stats = Instantiate(_characterStatsTemplate);
            
            // Handle augments
            _augments = new List<IAugment>();
            Augments = _augments.AsReadOnly();
            
            InitStateMachine();
        }

        void Start() {
            // Find and cache Input Actions
            FindInputAction("Strike", out _strikeAction);
            FindInputAction("ToggleAim", out _toggleAimAction);

            // Initialize character components
            Health.Initialize();
            Controller.Initialize();
            Aspect.Initialize(); // Require to be initialized before the mortar
            Mortar.Initialize();

            _toggleAimAction.started += Mortar.BeginAiming;
            _toggleAimAction.canceled += Mortar.EndAiming;
        }
        
        public void Reset()
        {
            // Reset the parent if it was held by an actor
            transform.SetParent(null);
            SceneManager.MoveGameObjectToScene(transform.gameObject, SceneManager.GetActiveScene());
            
            gameObject.SetActive(true);
            
            Health.Reset();
            Controller.Reset();
            Aspect.Reset();
            Mortar.Reset();

            _strikeState.Reset();
            
            _stateMachine.SetState(_locomotionState);
        }
        
        void OnDestroy() {
            _stateMachine.Dispose();

            Health.Dispose();
            Controller.Dispose();
            Aspect.Dispose();
            Mortar.Dispose();

            foreach (var effect in _activeEffects)
            {
                effect.OnCompleted -= RemoveEffect;
                effect.Cancel(this);
            }
            _activeEffects.Clear();
            
            if (_toggleAimAction != null && Mortar != null)
            {
                _toggleAimAction.started -= Mortar.BeginAiming;
                _toggleAimAction.canceled -= Mortar.EndAiming;
            }
        }

        private void InitStateMachine()
        {
            _stateMachine = new StateMachine();
            
            // Declare States
            _locomotionState = new LocomotionState(this, _animator);
            var aimState = new AimState(this, _animator);
            var shootState = new ShootState(this, _animator);
            _stunState = new StunState(this, _animator);
            _strikeState = new StrikeState(this, _animator);
            var deathState = new DeathState(this, _animator);
            
            // Define transitions
            At(_stunState, _locomotionState, new FuncPredicate(() => !_stunState.IsActive));
            At(_strikeState, _locomotionState, new FuncPredicate(() => _strikeState.IsFinished));
            At(_locomotionState, _strikeState, new FuncPredicate(() => _strikeAction.triggered && !_strikeState.InCooldown));
            At(_locomotionState, aimState, new GameplayFuncPredicate(() => _toggleAimAction.IsPressed()));
            At(aimState, _locomotionState, new GameplayFuncPredicate(() => !_toggleAimAction.IsPressed()));
            At(aimState, _strikeState, new GameplayFuncPredicate(() => _strikeAction.triggered && !_strikeState.InCooldown));
            At(aimState, shootState, new GameplayFuncPredicate(() => Mortar.IsShooting));
            At(shootState, aimState, new GameplayFuncPredicate(() => shootState.IsClipFinished));

            Any(deathState, new FuncPredicate(() => !Health.IsAlive));
            Any(_stunState, new FuncPredicate(() => _stunState.IsActive && Health.IsAlive));
            
            // Set initial state
            _stateMachine.SetState(_locomotionState);
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
        public void AddAugment(SO_Augment augmentData)
        {
            var augmentInstance = AugmentFactory.Create(augmentData, this, SystemManager.Config.AugmentDatabase); // TODO: DB Access can be improved
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
            Aspect.Update();
            Mortar.Update();
            
            UpdateAnimator();
        }

        private void FixedUpdate()
        {
            _stateMachine.FixedUpdate();
            
            Health.FixedUpdate();
            Controller.FixedUpdate();
            Aspect.FixedUpdate();
            Mortar.FixedUpdate();
        }

        private void OnDrawGizmos()
        {
            Health?.OnDrawGizmos();
            Controller?.OnDrawGizmos();
            Aspect?.OnDrawGizmos();
            Mortar?.OnDrawGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            Health?.OnDrawGizmosSelected();
            Controller?.OnDrawGizmosSelected();
            Aspect?.OnDrawGizmosSelected();
            Mortar?.OnDrawGizmosSelected();
        }
        #endregion
        
        private void UpdateAnimator()
        {
            _animator.SetFloat(_speedHash, Controller.SpeedRatio);
        }

        public void ApplyEffect(IEffect<PlayerCharacter> effect)
        {
            effect.OnCompleted += RemoveEffect;
            _activeEffects.Add(effect);
            effect.Apply(this);
            
        }

        private void RemoveEffect(IEffect<PlayerCharacter> effect)
        {
            effect.OnCompleted -= RemoveEffect;
            _activeEffects.Remove(effect);
        }
        
        
        private void At(IState from, IState to, IPredicate condition) => _stateMachine.AddTransition(from, to, condition);
        
        private void Any(IState to, IPredicate condition) => _stateMachine.AddAnyTransition(to, condition);
        
#if UNITY_EDITOR
        // Useful to show only when the stats are initialized per player and prevent thinking we have to assign it in the inspector
        private bool ShouldShowStats => Stats != null;
#endif
    }
}