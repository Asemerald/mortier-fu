using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MortierFu
{
    public class PlayerCharacter : MonoBehaviour
    {
        /// <summary>
        /// Set by the game mode when gameplay actions are allowed or not.
        /// </summary>
        public static bool AllowGameplayActions { get; set; }

        [Header("Mortar")] [SerializeField] private AimWidget _aimWidgetPrefab;
        [SerializeField] private Transform _firePoint;

        [Header("Aspect")] [SerializeField] private CharacterAspectMaterials[] _characterAspectMaterials;

        [Header("References")] [SerializeField]
        private Animator _animator;
        [SerializeField] private PlayerTauntFeedback _tauntFeedback;
        
        [SerializeField] private SO_CharacterStats _characterStatsTemplate;

        private StateMachine _stateMachine;

        private InputAction _strikeAction;
        private InputAction _toggleAimAction;
        private InputAction _tauntAction;

        public PlayerManager Owner { get; private set; }
        public HealthCharacterComponent Health { get; private set; }
        public ControllerCharacterComponent Controller { get; private set; }
        public AspectCharacterComponent Aspect { get; private set; }
        public MortarCharacterComponent Mortar { get; private set; }

        [field: SerializeField, Expandable, ShowIf("ShouldShowStats")]
        public SO_CharacterStats Stats { get; private set; }

        private List<IAugment> _augments = new();
        public ReadOnlyCollection<IAugment> Augments;

        private List<IEffect<PlayerCharacter>> _activeEffects = new();
        private List<Ability> PuddleAbilities; //TODO: Make it better

        private LocomotionState _locomotionState;
        private KnockbackState _knockbackState;
        private StunState _stunState;
        private StrikeState _strikeState;

        private readonly int _speedHash = Animator.StringToHash("Speed");

        public PlayerInput PlayerInput => Owner?.PlayerInput;

        public List<Ability> GetPuddleAbilities => PuddleAbilities;

        public float GetStrikeCooldownProgress => _strikeState.StrikeCooldownProgress;

        public void Initialize(PlayerManager owner)
        {
            if (owner == null)
            {
                Logs.LogError("Cannot initialize player with null Owner !");
                return;
            }

            Owner = owner;

            var playerIndex = owner.PlayerIndex;
            if (playerIndex < 0 || playerIndex >= _characterAspectMaterials.Length)
            {
                Logs.LogError($"Player index {playerIndex} is out of bounds for character aspect materials.");
                return;
            }

            Aspect.SetAspectMaterials(_characterAspectMaterials[owner.PlayerIndex]);
        }

        void Awake()
        {
            // Create character components
            Health = new HealthCharacterComponent(this);
            Controller = new ControllerCharacterComponent(this);
            Aspect = new AspectCharacterComponent(this);
            Mortar = new MortarCharacterComponent(this, _aimWidgetPrefab, _firePoint);

            // Create a unique instance of CharacterData for this character
            Stats = Instantiate(_characterStatsTemplate);

            // Handle augments
            _augments = new List<IAugment>();
            Augments = _augments.AsReadOnly();

            _activeEffects = new List<IEffect<PlayerCharacter>>();
            PuddleAbilities = new List<Ability>();

            InitStateMachine();
        }

        void Start()
        {
            // Find and cache Input Actions
            FindInputAction("Strike", out _strikeAction);
            FindInputAction("ToggleAim", out _toggleAimAction);
            FindInputAction("Taunt", out _tauntAction);

            // Initialize character components
            Health.Initialize();
            Controller.Initialize();
            Aspect.Initialize(); // Require to be initialized before the mortar
            Mortar.Initialize();
            //TEMP Initialiser l'aimindicator
            GetComponent<TEMP_AimIndicatorSystem>().Initialize();

            _toggleAimAction.started += Mortar.BeginAiming;
            _toggleAimAction.canceled += Mortar.EndAiming;

            _tauntAction.started += ctx => _tauntFeedback.PlayTauntAsync().Forget();
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

            var effectsCopy = new List<IEffect<PlayerCharacter>>(_activeEffects);

            foreach (var effect in effectsCopy)
            {
                effect.OnCompleted -= RemoveEffect;
                effect.Cancel(this);
            }

            _activeEffects.Clear();

            _strikeState.Reset();

            _stateMachine.SetState(_locomotionState);
        }

        void OnDestroy()
        {
            _stateMachine.Dispose();

            Health.Dispose();
            Controller.Dispose();
            Aspect.Dispose();
            Mortar.Dispose();

            if (_toggleAimAction == null || Mortar == null) return;

            _toggleAimAction.started -= Mortar.BeginAiming;
            _toggleAimAction.canceled -= Mortar.EndAiming;
        }

        private void InitStateMachine()
        {
            _stateMachine = new StateMachine();

            // Declare States
            _locomotionState = new LocomotionState(this, _animator);
            var aimState = new AimState(this, _animator);
            var shootState = new ShootState(this, _animator);
            _knockbackState = new KnockbackState(this, _animator);
            _stunState = new StunState(this, _animator);
            _strikeState = new StrikeState(this, _animator);
            var deathState = new DeathState(this, _animator);

            // Define transitions
            At(_knockbackState, _locomotionState, new FuncPredicate(() => !_knockbackState.IsActive));
            At(_stunState, _locomotionState, new FuncPredicate(() => !_stunState.IsActive));
            At(_strikeState, _locomotionState, new FuncPredicate(() => _strikeState.IsFinished));
            At(_locomotionState, _strikeState,
                new FuncPredicate(() => _strikeAction.triggered && !_strikeState.InCooldown));
            At(_locomotionState, aimState, new GameplayFuncPredicate(() => _toggleAimAction.IsPressed()));
            At(aimState, _locomotionState, new GameplayFuncPredicate(() => !_toggleAimAction.IsPressed()));
            At(aimState, _strikeState,
                new GameplayFuncPredicate(() => _strikeAction.triggered && !_strikeState.InCooldown));
            At(aimState, shootState, new GameplayFuncPredicate(() => Mortar.IsShooting));
            At(shootState, aimState, new GameplayFuncPredicate(() => shootState.IsClipFinished));

            Any(deathState, new FuncPredicate(() => !Health.IsAlive));
            Any(_knockbackState, new FuncPredicate(() => _knockbackState.IsActive && Health.IsAlive));
            Any(_stunState, new FuncPredicate(() => _stunState.IsActive && Health.IsAlive));

            // Set initial state
            _stateMachine.SetState(_locomotionState);
        }

        public void ReceiveKnockback(float duration, Vector3 force, float stunDuration)
        {
            force /= 1 + (Stats.GetAvatarSize() - 1) * Stats.AvatarSizeToForceMitigationFactor;
            _knockbackState.ReceiveKnockback(duration, force, stunDuration);
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
            var augmentInstance =
                AugmentFactory.Create(augmentData, this,
                    SystemManager.Config.AugmentDatabase); // TODO: DB Access can be improved
            augmentInstance.Initialize();
            _augments.Add(augmentInstance);
        }

        public void AddPuddleEffect(Ability ability)
        {
            if (!PuddleAbilities.Contains(ability)) //TODO: see later if we add duplicate or power up the effect
            {
                PuddleAbilities.Add(ability);
            }
        }

        public void RemovePuddleEffect(Ability ability)
        {
            PuddleAbilities.Remove(ability);
        }

        private bool HasEffect(IEffect<PlayerCharacter> effect) => _activeEffects.Contains(effect);

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

        private void OnCollisionEnter(Collision other)
        {
            if (_knockbackState.IsActive && other.impulse.magnitude > 5)
            {
                ReceiveStun(_knockbackState.StunDuration);
                //temp ajout destruction barriere
                if (other.gameObject.GetComponent<Breakable>())
                {
                    other.gameObject.GetComponent<Breakable>().Interact();
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Health?.OnDrawGizmos();
            Controller?.OnDrawGizmos();
            Aspect?.OnDrawGizmos();
            Mortar?.OnDrawGizmos();

            if (Owner != null)
            {
                Gizmos.color = Color.white;
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2,
                    $"Player {Owner.PlayerIndex + 1}");
            }
        }

        private void OnDrawGizmosSelected()
        {
            Health?.OnDrawGizmosSelected();
            Controller?.OnDrawGizmosSelected();
            Aspect?.OnDrawGizmosSelected();
            Mortar?.OnDrawGizmosSelected();

            if (Owner != null)
            {
                Gizmos.color = Color.white;
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2,
                    $"Player {Owner.PlayerIndex + 1}");
            }
        }

#endif

        #endregion

        private void UpdateAnimator()
        {
            _animator.SetFloat(_speedHash, Controller.SpeedRatio);
        }

        public void ApplyEffect(IEffect<PlayerCharacter> effect)
        {
            if (HasEffect(effect))
                return;

            _activeEffects.Add(effect);
            effect.OnCompleted += RemoveEffect;
            effect.Apply(this);
        }

        public void RemoveEffect(IEffect<PlayerCharacter> effect)
        {
            effect.OnCompleted -= RemoveEffect;
            _activeEffects.Remove(effect);
        }

        private void At(IState from, IState to, IPredicate condition) =>
            _stateMachine.AddTransition(from, to, condition);

        private void Any(IState to, IPredicate condition) => _stateMachine.AddAnyTransition(to, condition);

#if UNITY_EDITOR
        // Useful to show only when the stats are initialized per player and prevent thinking we have to assign it in the inspector
        private bool ShouldShowStats => Stats != null;
#endif
    }
}