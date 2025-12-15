using UnityEngine.InputSystem;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class ControllerCharacterComponent : CharacterComponent
    {
        [Header("Debug"), SerializeField] private Color _debugStrikeColor = Color.green;

        protected Rigidbody rigidbody;

        private Vector3 _moveDirection;
        
        private InputAction _moveAction;

        public float SpeedRatio => Mathf.Clamp01(rigidbody.linearVelocity.magnitude / Stats.MoveSpeed.Value); 
        
        public ControllerCharacterComponent(PlayerCharacter character) : base(character)
        {
            if (character == null) return;
            
            if (!character.TryGetComponent(out rigidbody))
            {
                Logs.LogError("[PlayerController]: Rigidbody component is required and missing.");
                return;
            }
        }

        public override void Initialize()
        {
            character.FindInputAction("Move", out _moveAction);

            Stats.AvatarSize.OnDirtyUpdated += UpdateAvatarSize;
            Stats.MaxHealth.OnDirtyUpdated += UpdateAvatarSize;
            UpdateAvatarSize();
        }

        public override void Reset()
        {
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }

        public override void Dispose() {
            Stats.AvatarSize.OnDirtyUpdated -= UpdateAvatarSize;
            Stats.MaxHealth.OnDirtyUpdated -= UpdateAvatarSize;
        }
        
        public override void Update()
        { }
        
        public override void FixedUpdate()
        { }

        private void UpdateAvatarSize()
        {
            character.transform.localScale = Vector3.one * Stats.GetAvatarSize();
        }
        
        // LocomotionState methods
        public void HandleMovementFixedUpdate()
        {
            Vector3 currentVelocity = rigidbody.linearVelocity;
            Vector3 targetVelocity = new Vector3(_moveDirection.x, rigidbody.linearVelocity.y, _moveDirection.y);

            // -------------------------------
            // 1) APPLICATION DU KNOCKBACK
            // -------------------------------
            if (_knockback.sqrMagnitude > 0.01f)
            {
                rigidbody.AddForce(_knockback, ForceMode.VelocityChange);

                // Le knockback décroit progressivement
                _knockback = Vector3.Lerp(_knockback, Vector3.zero, knockbackDecay * Time.fixedDeltaTime);
            }

            // -------------------------------
            // 2) CONTROLE DU JOUEUR MEME PENDANT LE KNOCKBACK
            // -------------------------------
            Vector3 velocityChange = (targetVelocity - currentVelocity);

            // Le contrôle est réduit quand knockback actif
            if (_knockback.sqrMagnitude > 0.01f)
                velocityChange *= moveDuringKnockbackFactor;

            velocityChange = Vector3.ClampMagnitude(velocityChange, character.Stats.MoveSpeed.Value);

            rigidbody.AddForce(velocityChange, ForceMode.VelocityChange);
        }

        public void HandleMovementUpdate(float factor = 1.0f) // TODO: Improve the speed factor
        {
            var horizontal = _moveAction.ReadValue<Vector2>().x;
            var vertical = _moveAction.ReadValue<Vector2>().y;
            
            var targetDirection = (new Vector2(horizontal, vertical)).normalized;
            var targetSpeed = Stats.MoveSpeed.Value * factor;

            // Targetted Velocity
            var targetVelocity = targetDirection * targetSpeed;

            // Accel/Decel if moving or not
            float rate = (targetDirection.sqrMagnitude > 0.01f) ? Stats.Accel.Value : Stats.Decel.Value;

            // Apply smooth Interpolation
            _moveDirection = Vector2.Lerp(_moveDirection, targetVelocity, Time.deltaTime * rate);
            
            var velocity3D = new Vector3(_moveDirection.x, 0f, _moveDirection.y);
            
            // Smooth rotation apply to character
            if (velocity3D.sqrMagnitude > 0.001f)
            {
                character.transform.rotation = Quaternion.Slerp(
                    character.transform.rotation,
                    Quaternion.LookRotation(velocity3D.normalized, Vector3.up),
                    Time.deltaTime * 10f
                );
            }
        }
        
        // Debugs
        public override void OnDrawGizmos()
        {
            Gizmos.color = _debugStrikeColor;
            Gizmos.DrawWireSphere(character.transform.position, Stats.GetStrikeRadius());
        }

        public void ResetVelocity()
        {
            rigidbody.linearVelocity = Vector3.zero;
        }

        private Vector3 _knockback;
        private float knockbackDecay = 2.2f;          // vitesse à laquelle le bump se dissipe
        private float moveDuringKnockbackFactor = 0.75f; 
        
        public void ApplyKnockback(Vector3 force)
        {
            rigidbody.AddForce(force, ForceMode.Impulse);
            _knockback = force;   // on remplace ou on ajoute selon ton besoin
        }
    }
}