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
            
            UpdateAvatarSize();
        }

        public override void Reset()
        { }

        public override void Dispose()
        { }
        
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
            
            _moveDirection = new Vector2(horizontal, vertical).normalized * (Stats.MoveSpeed.Value * factor);

            var lookDir = new Vector3(horizontal, 0f, vertical);
            
            if (lookDir.sqrMagnitude > 0.001f)
            {
                character.transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
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