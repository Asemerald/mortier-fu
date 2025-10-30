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
    public class ControllerCharacterComponent : CharacterComponent
    {
        [Header("Debug"), SerializeField] private Color _debugStrikeColor = Color.green;
        
        private CountdownTimer _strikeCooldownTimer;
        private CountdownTimer _strikeTriggerTimer;
        private CountdownTimer _stunTimer;
        
        private StateMachine _stateMachine;
        
        private Collider[] _overlapBuffer = new Collider[32];

        protected Rigidbody rigidbody;

        private Vector3 _moveDirection;
        
        private InputAction _moveAction;
        private InputAction _strikeAction;
        private InputAction _toggleAimAction;

        private void At(IState from, IState to, IPredicate condition) => _stateMachine.AddTransition(from, to, condition);
        private void Any(IState to, IPredicate condition) => _stateMachine.AddAnyTransition(to, condition);

        public ControllerCharacterComponent(PlayerCharacter playerCharacter) : base(playerCharacter)
        {
            if (playerCharacter == null) return;
            
            if (!playerCharacter.TryGetComponent(out rigidbody))
            {
                Logs.LogError("[PlayerController]: Rigidbody component is required and missing.");
                return;
            }
        }

        public override void Initialize()
        {
            // Find and cache Input Actions
            character.FindInputAction("Move", out _moveAction);
            character.FindInputAction("Strike", out _strikeAction);
            character.FindInputAction("ToggleAim", out _toggleAimAction);
            
            InitStateMachine(); // TODO: Move State Machine to the player character
            
            // Set up Timers
            _stunTimer = new CountdownTimer(Stats.StunDuration.Value);
            _strikeCooldownTimer = new CountdownTimer(Stats.StrikeCooldown.Value);
            _strikeTriggerTimer = new CountdownTimer(Stats.StrikeDuration.Value);
            
            UpdateAvatarSize();
        }

        public override void Reset()
        { }

        public override void Dispose()
        {
            _stunTimer.Dispose();
            _strikeCooldownTimer.Dispose();
            _strikeTriggerTimer.Dispose();
        }
        
        public override void Update()
        {
            _stateMachine.Update();
        }
        
        public override void FixedUpdate()
        {
            _stateMachine.FixedUpdate();
        }

        private void UpdateAvatarSize()
        {
            character.transform.localScale = Vector3.one * Stats.AvatarSize.Value;
        }
        
        // LocomotionState methods
        public void HandleMovementFixedUpdate()
        {
            var velocity = new Vector3(_moveDirection.x, rigidbody.linearVelocity.y, _moveDirection.y);
            rigidbody.linearVelocity = velocity;
        }

        public void HandleMovementUpdate(float factor = 1.0f) // TODO: Improve the speed factor
        {
            var horizontal = _moveAction.ReadValue<Vector2>().x;
            var vertical = _moveAction.ReadValue<Vector2>().y;
            
            _moveDirection = new Vector2(horizontal, vertical).normalized * (Stats.MoveSpeed.Value * factor);

            var lookDir = new Vector3(horizontal, 0f, vertical);
            
            if (lookDir.sqrMagnitude > 0.001f)
            {
                character.transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
            }
        }

        // DeathState methods
        public void EnterDeathState()
        {
            ResetVelocity();
            character.gameObject.SetActive(false);
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
            var origin = character.transform.position;
            var count = Physics.OverlapSphereNonAlloc(origin, Stats.StrikeRadius.Value, _overlapBuffer);

            // Pour éviter de détecter plusieurs fois les mêmes objets ou joueurs
            var processedRoots = new HashSet<GameObject>();
            
             for (var i = 0; i < count; i++)
             {
                 var hit = _overlapBuffer[i];
                 if (hit == null) continue;

                 
                 //var root = hit.character.transform.root.gameObject; // TODO ? Hit is collider, there is no access to character ?
                 var root = hit.transform.root.gameObject; // TODO ? Hit is collider, there is no access to character ?
                 
                 if (!processedRoots.Add(root)) continue;

                if (hit.TryGetComponent(out Bombshell bombshell))
                {
                    if (BombshellManager.Instance != null)
                        BombshellManager.Instance.RecycleBombshell(bombshell);
                    else
                        Logs.LogWarning("No BombshellManager instance available to recycle bombshell.");

                    continue;
                }

                var other = hit.GetComponentInParent<PlayerCharacter>();
                if (other == null) continue;
                if (other == character) continue;

                other.Controller.ReceiveStun();
                other.Health.TakeDamage(Stats.StrikeDamage.Value);
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
        public override void OnDrawGizmos()
        {
            Gizmos.color = _debugStrikeColor;
            Gizmos.DrawWireSphere(character.transform.position, Stats.StrikeRadius.Value);
        }

        private void ResetVelocity()
        {
            rigidbody.linearVelocity = Vector3.zero;
        }
        
        private void InitStateMachine()
        {
            // Declare States
            var locomotionState = new LocomotionState(character);
            var aimState = new AimState(character);
            var stunState = new StunState(character);
            var strikeState = new StrikeState(character);
            var deathState = new DeathState(character);
            
            // Define transitions
            At(stunState, locomotionState, new FuncPredicate(() => !_stunTimer.IsRunning));
            At(strikeState, locomotionState, new FuncPredicate(() => !_strikeTriggerTimer.IsRunning));
            At(locomotionState, strikeState, new FuncPredicate(() => _strikeAction.triggered && !_strikeCooldownTimer.IsRunning));
            At(locomotionState, aimState, new FuncPredicate(() => _toggleAimAction.IsPressed()));
            At(aimState, locomotionState, new FuncPredicate(() => !_toggleAimAction.IsPressed()));
            At(aimState, strikeState, new FuncPredicate(() => _strikeAction.triggered && !_strikeCooldownTimer.IsRunning));

            Any(deathState, new FuncPredicate(() => !character.Health.IsAlive));
            Any(stunState, new FuncPredicate(() => _stunTimer.IsRunning));
            
            // Set initial state
            _stateMachine.SetState(locomotionState);
        }
    }
}