using System.Collections.Generic;
using UnityEngine.InputSystem;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    // TODO du refacto pour que ça soit mieux.
    // Voir pour le timer du countdown puisque jamais je le stop.
    // Faire attention au fait que lorsque le joueur rejoint en appuyant sur la touche LEFT BUMPER alors cela fait son strike.
    public class PlayerController : MonoBehaviour
    {
        [Header("Strike Parameters"), SerializeField] private float _strikeRadius = 2f;
        [SerializeField] private float _strikeDuration = 0.2f;
        [SerializeField] private float _strikeCooldown = 2f;
        
        [Header("Stun Parameters"), SerializeField] private float _stunDuration = 0.5f;
        
        [Header("Debug"), SerializeField] private Color _debugStrikeColor = Color.green;
        
        private CountdownTimer _strikeCountdownTimer;
        private CountdownTimer _strikeTriggerTimer;
        private CountdownTimer _stunTimer;
        
        private Character _character;
        private PlayerInput _playerInput;
        private StateMachine _stateMachine;
        
        private Collider[] _overlapBuffer = new Collider[32];

        private Rigidbody _rb;

        private Vector3 _moveDirection;
        
        private InputAction _strikeAction;

        private bool _canStrike => !_strikeCountdownTimer.IsRunning && !_stunTimer.IsRunning && _character.Health.IsAlive;
        private bool _isStun => _stunTimer.IsRunning && _character.Health.IsAlive;
        
        public SO_CharacterStats CharacterStats { get; private set; }

        private void At(IState from, IState to, IPredicate condition) => _stateMachine.AddTransition(from, to, condition);
        private void Any(IState to, IPredicate condition) => _stateMachine.AddAnyTransition(to, condition);
        
        private void Awake()
        {
            // Get required components
            _rb = GetComponent<Rigidbody>();
            _playerInput = GetComponent<PlayerInput>();
            _strikeAction = _playerInput.actions.FindAction("Strike");
            
            // Set up Timers
            _stunTimer = new CountdownTimer(_stunDuration);
            _strikeCountdownTimer = new CountdownTimer(_strikeCooldown);
            _strikeTriggerTimer = new CountdownTimer(_strikeDuration);
            
            // State Machine
            _stateMachine = new StateMachine();
            
            // Declare States
            var locomotionState = new LocomotionState(this);
            var aimState = new AimState(this);
            var stunState = new StunState(this);
            var strikeState = new StrikeState(this);
            var deathState = new DeathState(this);
            
            // Define transitions
            At(stunState, locomotionState, new FuncPredicate(() => !_isStun));
            At(locomotionState, strikeState, new FuncPredicate(() => _strikeAction.triggered && _canStrike));
            At(strikeState, locomotionState, new FuncPredicate(() => !_strikeTriggerTimer.IsRunning && _character.Health.IsAlive));
            // Si en StrikeState alors pas de AimState
            //At(locomotionState, aimState, new FuncPredicate(() =>)); Si le joueur appuie sur le bouton d'aim
            //At(aimState, locomotionState, new FuncPredicate(() => )); Si le joueur appuie sur le bouton de tir

            Any(deathState, new FuncPredicate(() => !_character.Health.IsAlive));
            Any(stunState, new FuncPredicate(() => _isStun));
            
            // Set initial state
            _stateMachine.SetState(locomotionState);
        }
        
        void Start()
        {
            // Get components and update Avatar size
            if (!TryGetComponent(out _character))
            {
                Logs.LogError("PlayerController requires a Character component on the same GameObject.");
                return;
            }
            CharacterStats = _character.CharacterStats;
            UpdateAvatarSize();
        }

        private void OnEnable()
        {
            _playerInput.enabled = true;
        }

        private void OnDisable()
        {
            _playerInput.enabled = false;
        }
        
        private void Update()
        {
            _stateMachine.Update();
        }
        
        private void FixedUpdate()
        {
            _stateMachine.FixedUpdate();
        }

        private void UpdateAvatarSize()
        {
            transform.localScale = Vector3.one * CharacterStats.AvatarSize.Value;
        }
        
        // LocomotionState methods
        public void HandleMovementFixedUpdate()
        {
            var velocity = new Vector3(_moveDirection.x, _rb.linearVelocity.y, _moveDirection.y);
            _rb.linearVelocity = velocity;
        }

        public void HandleMovementUpdate()
        {
            var horizontal = _playerInput.actions["Move"].ReadValue<Vector2>().x;
            var vertical = _playerInput.actions["Move"].ReadValue<Vector2>().y;
            
            _moveDirection = new Vector2(horizontal, vertical).normalized * CharacterStats.MoveSpeed.Value;

            var lookDir = new Vector3(horizontal, 0f, vertical);
            
            if (lookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
            }
        }

        // DeathState methods
        public void HandleDeath()
        {
            _playerInput.enabled = false;
        }
        
        // StunState methods
        public void EnterStunState()
        {
            _playerInput.enabled = false;
        }

        public void ExitStunState()
        {
            _playerInput.enabled = true;
            _stunTimer.Stop();
        }
        
        // HitState methods
        public void EnterStrikeState()
        {
            _strikeTriggerTimer.Start();
            //_stunCountdownTimer.Reset();
            _strikeCountdownTimer.Start();
        }
        
        public void ExecuteStrike()
        {
            var origin = transform.position;
            var count = Physics.OverlapSphereNonAlloc(origin, _strikeRadius, _overlapBuffer);

            // Pour éviter de détecter plusieurs fois les mêmes objets ou joueurs
            var processedRoots = new HashSet<GameObject>();
            
             for (var i = 0; i < count; i++)
             {
                 var hit = _overlapBuffer[i];
                 if (hit == null) continue;

                 var root = hit.transform.root.gameObject;
                 if (processedRoots.Contains(root)) continue;
                 processedRoots.Add(root);

                if (hit.TryGetComponent(out Bombshell bombshell))
                {
                    if (BombshellManager.Instance != null)
                        BombshellManager.Instance.RecycleBombshell(bombshell);
                    else
                        Logs.LogWarning("No BombshellManager instance available to recycle bombshell.");

                    continue;
                }

                var other = hit.GetComponentInParent<PlayerController>();
                if (other == null) continue;
                if (other == this) continue;

                other.ReceiveStun();
                other._character.Health.TakeDamage(_character.CharacterStats.HitDamage.Value);
             }
        }

        private void ReceiveStun()
        {
            if (_stunTimer.IsRunning) return;
            
            _stunTimer.Start();
        }
        
        public void ExitStrikeState()
        {
            _strikeTriggerTimer.Stop();
        }

        // Debugs
        private void OnDrawGizmos()
        {
            Gizmos.color = _debugStrikeColor;
            Gizmos.DrawWireSphere(transform.position, _strikeRadius);
        }
    }
}