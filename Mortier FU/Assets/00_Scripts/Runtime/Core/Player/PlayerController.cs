using UnityEngine.InputSystem;
using MortierFu.Shared;
using UnityEngine;

// TODO : Ne vous inquiètez pas je vais refacto un max !
namespace MortierFu
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float _stunAreaAngle = 90f;
        [SerializeField] private float _stunDistance = 2f;
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
            
            _stunTimer.OnTimerStart += () => Logs.Log("stun started");
            _stunTimer.OnTimerStop += () => Logs.Log("stun ended start countdown");
            
            // State Machine
            _stateMachine = new StateMachine();
            
            // Declare States
            var locomotionState = new LocomotionState(this);
            var aimState = new AimState(this);
            var stunState = new StunState(this);
            var deathState = new DeathState(this);
            
            // Define transitions
            At(locomotionState, stunState, new FuncPredicate(() => _stunTimer.IsRunning));
            At(stunState, locomotionState, new FuncPredicate(() => !_stunTimer.IsRunning));
            //At(locomotionState, aimState, new FuncPredicate(() =>)); Si le joueur appuie sur le bouton d'aim
            //At(aimState, locomotionState, new FuncPredicate(() => )); Si le joueur appuie sur le bouton de tir
            //At(stunState, aimState, new FuncPredicate(() => )); Si le joueur appuie sur le bouton d'aim et qu'il n'appuie pas sur le bouton de stun
            //At(aimState, stunState, new FuncPredicate(() => )); Si le joueur appuie sur le bouton de stun
            Any(deathState, new FuncPredicate(() => !_character.Health.IsAlive));
            
            // Set initial state
            _stateMachine.SetState(locomotionState);
        }
        
        void Start()
        {
            if (!TryGetComponent(out _character))
            {
                Logs.LogError("PlayerController requires a Character component on the same GameObject.");
                return;
            }
            CharacterStats = _character.CharacterStats;
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
        }
        
        public void HandleMovementFixedUpdate()
        {
            var velocity = new Vector3(_moveDirection.x, _rb.linearVelocity.y, _moveDirection.y);
            _rb.linearVelocity = velocity;
        }

        public void HandleMovementUpdate()
        {
            UpdateAvatarSize();
            
            var horizontal = _playerInput.actions["Move"].ReadValue<Vector2>().x;
            var vertical = _playerInput.actions["Move"].ReadValue<Vector2>().y;
            
            _moveDirection = new Vector2(horizontal, vertical).normalized * CharacterStats.MoveSpeed.Value;

            var lookDir = new Vector3(horizontal, 0f, vertical);
            
            if (lookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
            }
        }

        public void HandleDeath()
        {
            _playerInput.enabled = false;
            Logs.Log("Player has died");
        }

        public void HandleStun()
        {
           
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = _debugStunColor;

            if (!(_stunDistance > 0f)) return;

            var origin = transform.position;
            var forward = transform.forward;
            var halfAngle = _stunAreaAngle * 0.5f;
            var leftDir = Quaternion.Euler(0f, -halfAngle, 0f) * forward;
            var rightDir = Quaternion.Euler(0f, halfAngle, 0f) * forward;

            Gizmos.DrawLine(origin, origin + leftDir * _stunDistance);
            Gizmos.DrawLine(origin, origin + rightDir * _stunDistance);

            var segments = 24;
            var prev = origin + leftDir * _stunDistance;
            for (var i = 1; i <= segments; i++)
            {
                var t = (float)i / segments;
                var angle = -halfAngle + t * _stunAreaAngle;
                var dir = Quaternion.Euler(0f, angle, 0f) * forward;
                var next = origin + dir * _stunDistance;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
        
        // TODO : Je continue demain je suis fatigué
        private void ExecuteStun()
        {
            if (_stunCountdownTimer.IsRunning) return;
            
            _stunCountdownTimer.Start();

            var origin = transform.position;
            var forward = transform.forward;
            var halfAngle = _stunAreaAngle * 0.5f;
            var leftDir = Quaternion.Euler(0f, -halfAngle, 0f) * forward;
            var rightDir = Quaternion.Euler(0f, halfAngle, 0f) * forward;
            
            var hits = Physics.OverlapSphere(origin, _stunDistance);
            foreach (var hit in hits)
            {
                var other = hit.GetComponentInParent<PlayerController>();
                if (other == null) continue;
                if (other == this) continue;

                var dir = other.transform.position - origin;
                dir.y = 0f;
                if (dir.sqrMagnitude <= 0.0001f) continue;

                var angle = Vector3.Angle(forward, dir.normalized);
                if (angle <= halfAngle)
                {
                    other.ReceiveStun(_stunDamage);
                }
            }
        }

        // TODO : Je continue demain je suis fatigué
        public void ReceiveStun(float damage)
        {
            _stunTimer.Start();

            _stunTimer.OnTimerStart += () => Logs.Log($"{name} stunned");
            _stunTimer.OnTimerStop += () => Logs.Log($"{name} stun ended");
            
            // TODO : Appliquer des dégâts via damage et il faut que cela provienne des character stats
        }
    }
}