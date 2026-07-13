using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MortierFu.Analytics;
using MortierFu.Shared;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MortierFu
{
    public class PlayerCharacter : MonoBehaviour
    {
        private static readonly object k_controlContextInvincibilitySource = new ControlContextInvincibilitySource();
        
        public PlayerControlContext ControlContext { get; private set; } = PlayerControlContext.Lobby;

        public PlayerActionPermissions ActionPermissions { get; private set; } = PlayerActionPermissions.FromContext(PlayerControlContext.Lobby);

        public bool CanMove => ActionPermissions.CanMove && Health is { IsAlive: true };
        public bool CanAim => ActionPermissions.CanAim && Health is { IsAlive: true };
        public bool CanShoot => ActionPermissions.CanShoot && Health is { IsAlive: true };
        public bool CanDash => ActionPermissions.CanDash && Health is { IsAlive: true };
        private bool CanTaunt => ActionPermissions.CanTaunt && Health is { IsAlive: true };

        [SerializeField] private PlayerTauntFeedback _tauntFeedback;

        [Header("Dash Trail")] [SerializeField]
        private GameObject _dashTrailPrefab;

        [Header("Mortar")] [SerializeField] private AimWidget _aimWidgetPrefab;
        [SerializeField] private Transform _firePoint;

        [Header("Aspect")] [SerializeField] private CharacterAspectMaterials[] _characterAspectMaterials;

        [Header("References")] [SerializeField]
        private Animator _animator;

        [SerializeField] private SO_CharacterStats _characterStatsTemplate;
        [SerializeField] private Transform _strikePoint;
        [SerializeField] private Transform _feetPoint;
        
        [Header("Customization")]
        [SerializeField] private PlayerCustomizationVisual _customizationVisual;

        public PlayerCustomizationVisual CustomizationVisual => _customizationVisual;
        private StateMachine _stateMachine;

        private InputAction _dashAction;
        private InputAction _toggleAimAction;
        
        private InputAction _tauntAction1;
        private InputAction _tauntAction2;
        private InputAction _tauntAction3;
        private InputAction _tauntAction4;

        public PlayerManager Owner { get; private set; }
        public HealthCharacterComponent Health { get; private set; }
        public ControllerCharacterComponent Controller { get; private set; }
        public AspectCharacterComponent Aspect { get; private set; }
        public MortarCharacterComponent Mortar { get; private set; }
        public SafeGroundCharacterComponent SafeGround { get; private set; }

        [field: SerializeField, Expandable, ShowIf("ShouldShowStats")]
        public SO_CharacterStats Stats { get; private set; }

        public Image TutorialImage;
        public TextMeshProUGUI TutorialText;

        private readonly List<SO_Augment> _ownedAugments = new();
        private readonly List<IAugment> _activeAugments = new();

        public ReadOnlyCollection<SO_Augment> OwnedAugments { get; private set; }
        public ReadOnlyCollection<IAugment> Augments { get; private set; }

        public bool AreAugmentsActive { get; private set; }

        // Assets specified by player color.
        [field: SerializeField] public SO_PlayerAssets Assets { get; private set; }

        private LocomotionState _locomotionState;
        private KnockbackState _knockbackState;
        private StunState _stunState;
        private DashState _dashState;

        private ShakeService _shakeService;

        private readonly int _speedHash = Animator.StringToHash("Speed");

        public PlayerInput PlayerInput => Owner?.PlayerInput;
        public ShakeService ShakeService => _shakeService;

        public float GetStrikeCooldownProgress => _dashState.DashCooldownProgress;
        public int AvailableDashCharges => _dashState.AvailableCharges;

        public Transform GetStrikePoint() => _strikePoint;
        public KnockbackState KnockbackState => _knockbackState;
        public Transform FeetPoint => _feetPoint;

        void Awake()
        {
            // Create character components
            Health = new HealthCharacterComponent(this);
            Controller = new ControllerCharacterComponent(this);
            Aspect = new AspectCharacterComponent(this);
            Mortar = new MortarCharacterComponent(this, _aimWidgetPrefab, _firePoint);
            SafeGround = new SafeGroundCharacterComponent(this);
            
            // Create a unique instance of CharacterData for this character
            Stats = Instantiate(_characterStatsTemplate);

            // Handle augments
            OwnedAugments = _ownedAugments.AsReadOnly();
            Augments = _activeAugments.AsReadOnly();
            
            ResolveCustomizationVisual();
            InitStateMachine();
        }

        void Start()
        {
            // Find and cache Input Actions
            FindInputAction("Dash", out _dashAction);
            FindInputAction("ToggleAim", out _toggleAimAction);
            
            FindInputAction("Taunt1", out _tauntAction1);
            FindInputAction("Taunt2", out _tauntAction2);
            FindInputAction("Taunt3", out _tauntAction3);
            FindInputAction("Taunt4", out _tauntAction4);

            // Initialize character components
            Health.Initialize();
            Controller.Initialize();
            Aspect.Initialize(); // Require to be initialized before the mortar
            Mortar.Initialize();
            SafeGround.Initialize();
            
            _toggleAimAction.started += Mortar.BeginAiming;
            _toggleAimAction.canceled += Mortar.EndAiming;

            _dashAction.started += PlayDashSFX;
            _tauntAction1.started += Taunt1;
            _tauntAction2.started += Taunt2;
            _tauntAction3.started += Taunt3;
            _tauntAction4.started += Taunt4;
            

            _shakeService = ServiceManager.Instance.Get<ShakeService>();

            _dashState.Reset();
            
            _speedMultiplier.Reset();
            _accelMultiplier.Reset();
            _decelMultiplier.Reset();
        }

        private void OnDisable() => Mortar?.CancelAiming();

        private void OnDestroy()
        {
            ClearAugments();

            _stateMachine?.Dispose();

            Health?.Dispose();
            Controller?.Dispose();
            Aspect?.Dispose();
            Mortar?.Dispose();
            SafeGround?.Dispose();
            
            if (_dashAction != null)
                _dashAction.started -= PlayDashSFX;

            if (_tauntAction1 != null)
                _tauntAction1.started -= Taunt1;
            if (_tauntAction2 != null)
                _tauntAction2.started -= Taunt2;
            if (_tauntAction3 != null)
                _tauntAction3.started -= Taunt3;
            if (_tauntAction4 != null)
                _tauntAction4.started -= Taunt4;

            if (_toggleAimAction == null || Mortar == null) return;
            _toggleAimAction.started -= Mortar.BeginAiming;
            _toggleAimAction.canceled -= Mortar.EndAiming;
        }

        public void Initialize(PlayerManager owner)
        {
            if (!owner)
            {
                Logs.LogError("Cannot initialize player with null Owner !");
                return;
            }

            Owner = owner;

            int playerIndex = owner.PlayerIndex;
            if (playerIndex < 0 || playerIndex >= _characterAspectMaterials.Length)
            {
                Logs.LogError($"Player index {playerIndex} is out of bounds for character aspect materials.");
                return;
            }

            Aspect.SetAspectMaterials(_characterAspectMaterials[owner.PlayerIndex]);

            ResolveCustomizationVisual();

            if (_customizationVisual)
                _customizationVisual.Apply(owner.Customization);
            else
                Logs.LogWarning("[PlayerCharacter] No PlayerCustomizationVisual assigned.", this);

            // Now that player materials are populated to the Aspect Component, we can initialize the trail.
            _dashState.InitializeTrail(_dashTrailPrefab);
        }

        public void RefreshCustomizationFromOwner()
        {
            if (!Owner)
                return;

            ResolveCustomizationVisual();

            if (_customizationVisual)
                _customizationVisual.Apply(Owner.Customization);
        }
        
        public void SetControlContext(PlayerControlContext context)
        {
            ControlContext = context;
            ActionPermissions = PlayerActionPermissions.FromContext(context);

            UpdateInvincibilityFromControlContext(context);

            if (!CanAim)
            {
                Mortar?.CancelAiming();
            }

            if (!CanMove)
            {
                Controller?.ResetVelocity();
            }
        }

        public void Reset()
        {
            gameObject.SetActive(true);
            
            // Reset the parent if it was held by an actor
            transform.SetParent(null);
            SceneManager.MoveGameObjectToScene(transform.gameObject, SceneManager.GetActiveScene());
            
            Health.Reset();
            Controller.Reset();
            Aspect.Reset();
            Mortar.Reset();

            _knockbackState.Reset();
            _dashState.Reset();

            _stateMachine.SetState(_locomotionState);

            _speedMultiplier.Reset();
            _accelMultiplier.Reset();
            _decelMultiplier.Reset();
            
            RefreshRuntimeAfterAugmentStateChanged();
        }

        public void RespawnAt(Vector3 position, Quaternion rotation)
        {
            Mortar?.CancelAiming();

            Reset();

            transform.SetPositionAndRotation(position, rotation);

            Controller?.ResetVelocity();

            Logs.Log($"[PlayerCharacter] Respawned {name} at {position}.");
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
            _dashState = new DashState(this, _animator);
            var deathState = new DeathState(this, _animator);

            // Define transitions

            At(_knockbackState, _locomotionState, new FuncPredicate(() => !_knockbackState.IsActive));

            At(_stunState, _locomotionState, new FuncPredicate(() => !_stunState.IsActive));

            At(_dashState, _locomotionState, new FuncPredicate(() => _dashState.IsFinished));

            // Locomotion -> Dash
            At(_locomotionState, _dashState, new PlayerActionPredicate(this, permissions => permissions.CanDash, () => _dashAction.triggered 
                && _dashState.AvailableCharges > 0 && Controller.GetDashDirection().sqrMagnitude > 0.01f));

            // Locomotion -> Aim
            At(_locomotionState, aimState, new PlayerActionPredicate(this, permissions => permissions.CanAim, () => _toggleAimAction.IsPressed()));

            // Aim -> Locomotion
            At(aimState, _locomotionState, new FuncPredicate(() => !_toggleAimAction.IsPressed() || !CanAim));

            // Aim -> Dash
            At(aimState, _dashState, new PlayerActionPredicate(this, permissions => permissions.CanDash, () => _dashAction.triggered
                && _dashState.AvailableCharges > 0 && Controller.GetDashDirection().sqrMagnitude > 0.01f));

            // Aim -> Shoot
            At(aimState, shootState, new PlayerActionPredicate(this, permissions => permissions.CanShoot, () => Mortar.IsShooting));

            // Shoot -> Aim
            At(shootState, aimState, new FuncPredicate(() => shootState.IsClipFinished || !CanShoot));

            // Transitions globales prioritaires
            Any(deathState, new FuncPredicate(() => !Health.IsAlive));

            Any(_knockbackState, new FuncPredicate(() => _knockbackState.IsActive && !_stunState.IsActive && Health.IsAlive));

            Any(_stunState, new FuncPredicate(() => _stunState.IsActive && Health.IsAlive));

            // Set initial state
            _stateMachine.SetState(_locomotionState);
        }

        public void ReceiveKnockback(float duration, Vector3 force, float stunDuration, object source)
        {
            force /= 1 + (Stats.GetAvatarSize() - 1) * Stats.AvatarSizeToForceMitigationFactor;
            _knockbackState.ReceiveKnockback(duration, force, stunDuration, source);
        }

        public void ReceiveStun(float duration) => _stunState.ReceiveStun(duration);

        public void FindInputAction(string actionName, out InputAction action)
        {
#if UNITY_EDITOR
            bool isEditor = true;
#else
            bool isEditor = false;
#endif
            action = PlayerInput.actions.FindAction(actionName, isEditor);
            if (action == null)
                Logs.LogError($"[PlayerCharacter]: Input Action '{actionName}' not found in PlayerInput actions.");
        }

        #region Augments

        public void AddAugment(SO_Augment augmentData)
        {
            if (!augmentData)
                return;

            _ownedAugments.Add(augmentData);

            if (AreAugmentsActive)
                ActivateAugment(augmentData);

            AnalyticsSystem analyticsSystem = SystemManager.Instance.Get<AnalyticsSystem>();
            analyticsSystem?.OnAugmentSelected(this, augmentData);
        }

        public void ActivateRoundAugments()
        {
            if (AreAugmentsActive)
                return;

            AreAugmentsActive = true;

            for (int i = 0; i < _ownedAugments.Count; i++)
                ActivateAugment(_ownedAugments[i]);

            RefreshRuntimeAfterAugmentStateChanged();
        }

        private void DeactivateRoundAugments()
        {
            if (!AreAugmentsActive && _activeAugments.Count == 0)
            {
                RefreshRuntimeAfterAugmentStateChanged();
                return;
            }

            DisposeActiveAugments();

            AreAugmentsActive = false;

            RefreshRuntimeAfterAugmentStateChanged();
        }

        public void ClearAugments()
        {
            DeactivateRoundAugments();
            _ownedAugments.Clear();
        }

        private void ActivateAugment(SO_Augment augmentData)
        {
            if (!augmentData)
                return;

            try
            {
                IAugment augmentInstance = AugmentFactory.Create(augmentData, this, SystemManager.Config.AugmentDatabase);
                augmentInstance.Initialize();
                _activeAugments.Add(augmentInstance);
            }
            catch (Exception e)
            {
                Logs.LogError($"[PlayerCharacter] Failed to activate augment '{augmentData.name}': {e.Message}", this);
            }
        }

        private void DisposeActiveAugments()
        {
            for (int i = _activeAugments.Count - 1; i >= 0; i--)
            {
                try
                {
                    _activeAugments[i]?.Dispose();
                }
                catch (Exception e)
                {
                    Logs.LogError($"[PlayerCharacter] Failed to dispose augment '{_activeAugments[i]?.GetType().Name}': {e.Message}", this);
                }
            }

            _activeAugments.Clear();
        }
        
        public void ResetForRace()
        {
            DisposeActiveAugments();

            AreAugmentsActive = false;

            Stats.ClearAllModifiers();

            Reset();

            Logs.Log($"[PlayerCharacter] Reset for race: Player {Owner?.PlayerIndex + 1}.", this);
        }

        private void RefreshRuntimeAfterAugmentStateChanged()
        {
            Health?.RefreshFromStats();
            Health?.Reset();

            Mortar?.Reset();
            _dashState?.Reset();
        }

        #endregion

        #region Propagate Unity messages to character components

        private void Update()
        {
            _stateMachine.Update();

            Health.Update();
            Controller.Update();
            Aspect.Update();
            Mortar?.Update();
            SafeGround.Update();
            
            UpdateAnimator();
        }

        private void FixedUpdate()
        {
            _stateMachine.FixedUpdate();

            Health.FixedUpdate();
            Controller.FixedUpdate();
            Aspect.FixedUpdate();
            Mortar?.FixedUpdate();
        }

        private void OnCollisionEnter(Collision other)
        {
            // C'est affreux
            if (_knockbackState.IsActive && other.impulse.magnitude > 5 &&
                (_knockbackState.LastBumpSource is not PlayerCharacter character ||
                 !other.gameObject.TryGetComponent<PlayerCharacter>(out var otherChar) || character != otherChar))
            {
                ReceiveStun(_knockbackState.StunDuration);

                if (other.rigidbody && other.rigidbody.TryGetComponent<Breakable>(out var breakable))
                {
                    breakable.Interact(other.GetContact(0).point);
                }

                if (_knockbackState.LastBumpSource == null) return;
                
                EventBus<TriggerSuccessfulPush>.Raise(new TriggerSuccessfulPush()
                {
                    Character = this,
                    Source = _knockbackState.LastBumpSource,
                });

                _knockbackState.ClearLastBumpSource();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Health?.OnDrawGizmos();
            Controller?.OnDrawGizmos();
            Aspect?.OnDrawGizmos();
            Mortar?.OnDrawGizmos();

            if (!Owner) return;
            
            Gizmos.color = Color.white;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2, $"Player {Owner.PlayerIndex + 1}");
        }

        private void OnDrawGizmosSelected()
        {
            Health?.OnDrawGizmosSelected();
            Controller?.OnDrawGizmosSelected();
            Aspect?.OnDrawGizmosSelected();
            Mortar?.OnDrawGizmosSelected();

            if (!Owner) return;
            
            Gizmos.color = Color.white;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2, $"Player {Owner.PlayerIndex + 1}");
        }

#endif

        #endregion

        private void UpdateAnimator() => _animator.SetFloat(_speedHash, Controller.SpeedRatio);

        private void PlayDashSFX(InputAction.CallbackContext context)
        {
            if (!CanDash)
                return;

            if (_dashState.DashCooldownProgress > 0f)
                AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Strike_Cant, transform.position);
        }

        public void WinRoundDance() => _animator.CrossFade("WinRound", 0.1f, 0);

        private void UpdateInvincibilityFromControlContext(PlayerControlContext context)
        {
            if (Health == null)
                return;

            if (ShouldBeInvincibleInContext(context))
                Health.AddInvincibility(k_controlContextInvincibilitySource);
            else
                Health.RemoveInvincibility(k_controlContextInvincibilitySource);
        }

        private static bool ShouldBeInvincibleInContext(PlayerControlContext context)
        {
            return context is PlayerControlContext.LobbyCustomization
                or PlayerControlContext.LobbySettingsOwner
                or PlayerControlContext.LobbyReturnConfirmationOwner;
        }

        private sealed class ControlContextInvincibilitySource
        { }

        private void At(IState from, IState to, IPredicate condition) => _stateMachine.AddTransition(from, to, condition);

        private void Any(IState to, IPredicate condition) => _stateMachine.AddAnyTransition(to, condition);

        private void Taunt1(InputAction.CallbackContext ctx)
        {
            if (!CanTaunt)
                return;

            _tauntFeedback.Taunt(1);
        }
        
        private void Taunt2(InputAction.CallbackContext ctx)
        {
            if (!CanTaunt)
                return;

            _tauntFeedback.Taunt(2);
        }
        
        private void Taunt3(InputAction.CallbackContext ctx)
        {
            if (!CanTaunt)
                return;

            _tauntFeedback.Taunt(3);
        }
        
        private void Taunt4(InputAction.CallbackContext ctx)
        {
            if (!CanTaunt)
                return;

            _tauntFeedback.Taunt(4);
        }

        private void ResolveCustomizationVisual()
        {
            if (_customizationVisual)
                return;

            _customizationVisual = GetComponentInChildren<PlayerCustomizationVisual>(true);
        }

        private readonly SmoothedMultiplier _speedMultiplier = new();
        private readonly SmoothedMultiplier _accelMultiplier = new();
        private readonly SmoothedMultiplier _decelMultiplier = new();

        public float ExternalSpeedMultiplier => _speedMultiplier.Value;
        public float ExternalAccelerationMultiplier => _accelMultiplier.Value;
        public float ExternalDecelerationMultiplier => _decelMultiplier.Value;

        public void SetExternalSpeedMultiplier(float target, float duration) => _speedMultiplier.SetTarget(target, duration, this);

        public void SetExternalAccelerationMultiplier(float target, float duration) => _accelMultiplier.SetTarget(target, duration, this);

        public void SetExternalDecelerationMultiplier(float target, float duration) => _decelMultiplier.SetTarget(target, duration, this);

        public bool CanPlayerInteractWithBombShell()
        {
            if (!Health.IsAlive) return false;
            return ControlContext is not (PlayerControlContext.AugmentRaceBullyClassic 
                or PlayerControlContext.AugmentRaceBullyMoveOnly 
                or PlayerControlContext.AugmentRaceBullyShootOnly);
        }
    }
}