using System.Collections.Generic;
using UnityEngine.InputSystem;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    // TODO du refacto pour que ça soit mieux.
    // Voir pour le timer du countdown puisque jamais je le stop.
    // Faire attention au fait que lorsque le joueur rejoint en appuyant sur la touche LEFT BUMPER alors cela fait son strike.
    // Déplacer dans le Character toute la state machine.
    public class PlayerController : MonoBehaviour
    {
        [Header("Debug"), SerializeField] private Color _debugStrikeColor = Color.green;
        
        private CountdownTimer _strikeCooldownTimer;
        private CountdownTimer _strikeTriggerTimer;
        private CountdownTimer _stunTimer;
        
        private Character _character;
        private StateMachine _stateMachine;
        private Mortar _mortar;
        
        private Collider[] _overlapBuffer = new Collider[32];

        private Rigidbody _rb;

        private Vector3 _moveDirection;
        
        private InputAction _moveAction;
        private InputAction _strikeAction;
        private InputAction _toggleAimAction;

        public SO_CharacterStats CharacterStats { get; private set; }
        
        public Mortar Mortar => _mortar;

        private void At(IState from, IState to, IPredicate condition) => _stateMachine.AddTransition(from, to, condition);
        private void Any(IState to, IPredicate condition) => _stateMachine.AddAnyTransition(to, condition);
        
        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _mortar = GetComponent<Mortar>();
            // Get components and update Avatar size
            if (!TryGetComponent(out _character))
            {
                Logs.LogError("PlayerController requires a Character component on the same GameObject.");
                return;
            }
            
            // Set up Timers
            _stunTimer = new CountdownTimer(CharacterStats.StunDuration.Value);
            _strikeCooldownTimer = new CountdownTimer(CharacterStats.StrikeCooldown.Value);
            _strikeTriggerTimer = new CountdownTimer(CharacterStats.StrikeDuration.Value);
        }
        
        void Start() 
        {
            CharacterStats = _character.CharacterStats;
            UpdateAvatarSize();
            
            // Find the move action from PlayerInput
            var playerInput = _character.PlayerInput;
            
            _moveAction = playerInput.actions.FindAction("Move");
            if (_moveAction == null)
            {
                Logs.LogError("[PlayerController]: 'Move' action not found in PlayerInput actions.");
            }

            _strikeAction = playerInput.actions.FindAction("Strike");
            if (_strikeAction == null)
            {
                Logs.LogError("[PlayerController]: 'Strike' action not found in PlayerInput actions.");
            }
            
            _toggleAimAction = playerInput.actions.FindAction("ToggleAim");
            if (_toggleAimAction == null)
            {
                Logs.LogError("[PlayerController]: 'ToggleAim' action not found in PlayerInput actions.");
            }
            
            // State Machine
            _stateMachine = new StateMachine();
            
            // Declare States
            var locomotionState = new LocomotionState(this);
            var aimState = new AimState(this);
            var stunState = new StunState(this);
            var strikeState = new StrikeState(this);
            var deathState = new DeathState(this);
            
            // Define transitions
            At(stunState, locomotionState, new FuncPredicate(() => !_stunTimer.IsRunning));
            At(strikeState, locomotionState, new FuncPredicate(() => !_strikeTriggerTimer.IsRunning));
            At(locomotionState, strikeState, new FuncPredicate(() => _strikeAction.triggered && !_strikeCooldownTimer.IsRunning));
            At(locomotionState, aimState, new FuncPredicate(() => _toggleAimAction.IsPressed()));
            At(aimState, locomotionState, new FuncPredicate(() => !_toggleAimAction.IsPressed()));
            At(aimState, strikeState, new FuncPredicate(() => _strikeAction.triggered && !_strikeCooldownTimer.IsRunning));

            Any(deathState, new FuncPredicate(() => !_character.Health.IsAlive));
            Any(stunState, new FuncPredicate(() => _stunTimer.IsRunning));
            
            // Set initial state
            _stateMachine.SetState(locomotionState);
            
            _character.PlayerInput.enabled = true; 
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

        public void HandleMovementUpdate(float factor = 1.0f) // TODO: Improve the speed factor
        {
            var horizontal = _moveAction.ReadValue<Vector2>().x;
            var vertical = _moveAction.ReadValue<Vector2>().y;
            
            _moveDirection = new Vector2(horizontal, vertical).normalized * (CharacterStats.MoveSpeed.Value * factor);

            var lookDir = new Vector3(horizontal, 0f, vertical);
            
            if (lookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
            }
        }

        // DeathState methods
        public void EnterDeathState()
        {
            ResetVelocity();
            gameObject.SetActive(false);
        }
        
        // StunState methods
        public void EnterStunState()
        {
            ResetVelocity();
        }

        public void ExitStunState()
        {
            _stunTimer.Stop();
        }
        
        // HitState methods
        public void EnterStrikeState()
        {
            _strikeTriggerTimer.Start();
            //_stunCountdownTimer.Reset();
            _strikeCooldownTimer.Start();
        }
        
        public void ExecuteStrike()
        {
            var origin = transform.position;
            var count = Physics.OverlapSphereNonAlloc(origin, CharacterStats.StrikeRadius.Value, _overlapBuffer);

            // Pour éviter de détecter plusieurs fois les mêmes objets ou joueurs
            var processedRoots = new HashSet<GameObject>();
            
             for (var i = 0; i < count; i++)
             {
                 var hit = _overlapBuffer[i];
                 if (hit == null) continue;

                 var root = hit.transform.root.gameObject;
                 
                 if (!processedRoots.Add(root)) continue;

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
                other._character.Health.TakeDamage(_character.CharacterStats.StrikeDamage.Value);
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
            Gizmos.DrawWireSphere(transform.position, CharacterStats.StrikeRadius.Value);
        }

        private void ResetVelocity()
        {
            _rb.linearVelocity = Vector3.zero;
        }
    }
}