using UnityEngine.InputSystem;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    // TODO du refacto pour que ça soit mieux.
    // Voir pour le timer du countdown puisque jamais je le stop.
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float _stunAreaAngle = 90f;
        [SerializeField] private float _stunDistanceMultiplier = 2f;
        [SerializeField] private float _stunDamage = 0f;
        
        [SerializeField] private float _stunDuration = 0.5f;
        [SerializeField] private float _stunTriggerDelay = 0.2f;
        [SerializeField] private float _stunCooldown = 2f;
        
        [Header("Debug"), SerializeField] private Color _debugStunColor = Color.green;
        
        private Character _character;
        private Vector3 _moveDirection;
        private PlayerInput _playerInput;

        private Rigidbody _rb;
        
        private StateMachine _stateMachine;
        
        private CountdownTimer _stunTimer;
        private CountdownTimer _stunCountdownTimer;
        private CountdownTimer _stunTriggerTimer;
        
        private float _currentStunDistance = 0f;
        
        private bool _canStun => !_stunCountdownTimer.IsRunning && !_stunTimer.IsRunning && _character.Health.IsAlive;

        private bool _isStun => _stunTimer.IsRunning && _character.Health.IsAlive;

        public SO_CharacterStats CharacterStats { get; private set; }

        private void At(IState from, IState to, IPredicate condition) => _stateMachine.AddTransition(from, to, condition);
        private void Any(IState to, IPredicate condition) => _stateMachine.AddAnyTransition(to, condition);
        
        private void Awake()
        {
            // Get required components
            _rb = GetComponent<Rigidbody>();
            _playerInput = GetComponent<PlayerInput>();
            
            // Set up Timers
            _stunTimer = new CountdownTimer(_stunDuration);
            _stunCountdownTimer = new CountdownTimer(_stunCooldown);
            _stunTriggerTimer = new CountdownTimer(_stunTriggerDelay);
            
            // State Machine
            _stateMachine = new StateMachine();
            
            // Declare States
            var locomotionState = new LocomotionState(this);
            var aimState = new AimState(this);
            var stunState = new StunState(this);
            var hitState = new HitState(this);
            var deathState = new DeathState(this);
            
            // Define transitions
            At(stunState, locomotionState, new FuncPredicate(() => !_isStun));
            At(locomotionState, hitState, new FuncPredicate(() => _playerInput.actions["Attack"].triggered && _canStun));
            At(hitState, locomotionState, new FuncPredicate(() => !_stunTriggerTimer.IsRunning && _character.Health.IsAlive));
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
            var attackAction = _playerInput?.actions["Attack"];
            
            if (attackAction != null && attackAction.triggered && !_stunCountdownTimer.IsRunning)
            {
                _stunCountdownTimer.Stop();
                
                _stunTriggerTimer.Start();
            }
            
            _stateMachine.Update();
        }
        
        private void FixedUpdate()
        {
            _stateMachine.FixedUpdate();
        }

        private void UpdateAvatarSize()
        {
            transform.localScale = Vector3.one * CharacterStats.AvatarSize.Value;
            _currentStunDistance = Mathf.Max(0.01f, CharacterStats.AvatarSize.Value * _stunDistanceMultiplier);
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
        public void EnterHitState()
        {
            _stunTriggerTimer.Start();
            _stunCountdownTimer.Start();
        }
        
        public void ExecuteStun()
        {
            var origin = transform.position;
            var forward = transform.forward;
            var halfAngle = _stunAreaAngle * 0.5f;
            
            var hits = Physics.OverlapSphere(origin, _currentStunDistance);
            foreach (var hit in hits)
            {
                var other = hit.GetComponentInParent<PlayerController>();
                if (other == null) continue;
                if (other == this) continue;

                var dir = other.transform.position - origin;
                dir.y = 0f;
                if (dir.sqrMagnitude <= 0.0001f) continue;

                var angle = Vector3.Angle(forward, dir.normalized);
                
                if (!(angle <= halfAngle)) continue;
                other.ReceiveStun();
                other._character.Health.TakeDamage(_character.CharacterStats.HitDamage.Value);
            }
        }

        private void ReceiveStun()
        {
            if (_stunTimer.IsRunning) return;
            
            _stunTimer.Start();
        }
        
        public void ExitHitState()
        {
            _stunTriggerTimer.Stop();
        }

        // Debugs
        private void OnDrawGizmos() 
        { 
            Gizmos.color = _debugStunColor;
            
            if (!(_currentStunDistance > 0f)) return;
            
            var origin = transform.position; 
            var forward = transform.forward; 
            var halfAngle = _stunAreaAngle * 0.5f; 
            var leftDir = Quaternion.Euler(0f, -halfAngle, 0f) * forward; 
            var rightDir = Quaternion.Euler(0f, halfAngle, 0f) * forward;
            
            Gizmos.DrawLine(origin, origin + leftDir * _currentStunDistance); 
            Gizmos.DrawLine(origin, origin + rightDir * _currentStunDistance);
            
            var segments = 24; 
            var prev = origin + leftDir * _currentStunDistance; 
            for (var i = 1; i <= segments; i++) 
            { 
                var t = (float)i / segments; 
                var angle = -halfAngle + t * _stunAreaAngle; 
                var dir = Quaternion.Euler(0f, angle, 0f) * forward;
                var next = origin + dir * _currentStunDistance; 
                Gizmos.DrawLine(prev, next); 
                prev = next; 
            } 
        }
        
        public void ExitHitState()
        {
            _stunTriggerTimer.Stop();
        }

        // Debugs
        private void OnDrawGizmos() 
        { 
            Gizmos.color = _debugStunColor;
            
            if (!(_currentStunDistance > 0f)) return;
            
            var origin = transform.position; 
            var forward = transform.forward; 
            var halfAngle = _stunAreaAngle * 0.5f; 
            var leftDir = Quaternion.Euler(0f, -halfAngle, 0f) * forward; 
            var rightDir = Quaternion.Euler(0f, halfAngle, 0f) * forward;
            
            Gizmos.DrawLine(origin, origin + leftDir * _currentStunDistance); 
            Gizmos.DrawLine(origin, origin + rightDir * _currentStunDistance);
            
            var segments = 24; 
            var prev = origin + leftDir * _currentStunDistance; 
            for (var i = 1; i <= segments; i++) 
            { 
                var t = (float)i / segments; 
                var angle = -halfAngle + t * _stunAreaAngle; 
                var dir = Quaternion.Euler(0f, angle, 0f) * forward;
                var next = origin + dir * _currentStunDistance; 
                Gizmos.DrawLine(prev, next); 
                prev = next; 
            } 
        }
    }
}