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
            UpdateAvatarSize();
        }

        public override void Reset()
        { }

        public override void Dispose() {
            Stats.AvatarSize.OnDirtyUpdated -= UpdateAvatarSize;
        }
        
        public override void Update()
        { }
        
        public override void FixedUpdate()
        { }

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
            
            var targetDirection = (new Vector2(horizontal, vertical)).normalized;
            var targetSpeed = Stats.MoveSpeed.Value * factor;

            // On veut atteindre cette vitesse finale
            var targetVelocity = targetDirection * targetSpeed;

            // Accélération ou décélération selon si on bouge ou non
            float rate = (targetDirection.sqrMagnitude > 0.01f) ? Stats.Accel.Value : Stats.Decel.Value;

            // Applique Interpolation douce (lissage)
            _moveDirection = Vector2.Lerp(_moveDirection, targetVelocity, Time.deltaTime * rate);
            
            var velocity3D = new Vector3(_moveDirection.x, 0f, _moveDirection.y);
            
            if (velocity3D.sqrMagnitude > 0.001f)
            {
                character.transform.rotation = Quaternion.Slerp(
                    character.transform.rotation,
                    Quaternion.LookRotation(velocity3D.normalized, Vector3.up),
                    Time.deltaTime * 10f // vitesse de rotation fluide
                );
            }
        }
        
        // Debugs
        public override void OnDrawGizmos()
        {
            Gizmos.color = _debugStrikeColor;
            Gizmos.DrawWireSphere(character.transform.position, Stats.StrikeRadius.Value);
        }

        public void ResetVelocity()
        {
            rigidbody.linearVelocity = Vector3.zero;
        }
    }
}