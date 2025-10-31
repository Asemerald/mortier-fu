using UnityEngine.InputSystem;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class ControllerCharacterComponent : CharacterComponent
    {
        private readonly Rigidbody _rigidbody; 
        private InputAction _moveAction;
        private Vector3 _moveVector;

        public ControllerCharacterComponent(PlayerCharacter playerCharacter) : base(playerCharacter)
        {
            if (playerCharacter == null) return;
            
            if (!playerCharacter.TryGetComponent(out _rigidbody))
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

        public override void Update()
        {
            // Update the linear damping
            _rigidbody.linearDamping = Stats.MoveDrag.Value;
            UpdateAvatarSize();
        }

        public override void Reset()
        { }

        public override void Dispose()
        { }
        
        /// <summary>
        /// Should be called in the FixedUpdate
        /// </summary>
        public void HandleMovement() // TODO: Implement Speed factor to limit speed during certain actions by a given amount
        {
            // Read input
            Vector2 inputVector = _moveAction.ReadValue<Vector2>();
            _moveVector = new Vector3(inputVector.x, 0f, inputVector.y).normalized;
            if (inputVector == Vector2.zero) return;

            float acceleration = Stats.MoveAcceleration.Value;
            Vector3 accelerationVector = _moveVector * acceleration;
            _rigidbody.AddForce(accelerationVector, ForceMode.Force);
            
            LimitVelocity();
        }

        private void LimitVelocity()
        {
            Vector3 groundVelocity = _rigidbody.linearVelocity.With(y: 0f);
            float maxSpeed = Stats.MoveSpeed.Value;
            if (groundVelocity.magnitude > maxSpeed)
            {
                Vector3 clampedGroundVelocity = groundVelocity.normalized * maxSpeed;
                _rigidbody.linearVelocity = clampedGroundVelocity.With(y: _rigidbody.linearVelocity.y);
                Logs.Log($"[ControllerCharacterComponent]: Clamping velocity to max speed {_rigidbody.linearVelocity.magnitude}.");
            }
        }
        
        /// <summary>
        /// Should be called in the FixedUpdate
        /// </summary>
        public void HandleRotation()
        {
            if (_moveVector.sqrMagnitude < 0.01f) return;

            Quaternion lookRotation = Quaternion.LookRotation(_moveVector, Vector3.up);
            _rigidbody.MoveRotation(lookRotation);
        }

        public void ResetVelocity()
        {
            _rigidbody.linearVelocity = Vector3.zero;
        }

        private void UpdateAvatarSize()
        {
            float scale = Stats.AvatarSize.Value;
            character.transform.localScale = Vector3.one * scale;
        }
    }
}