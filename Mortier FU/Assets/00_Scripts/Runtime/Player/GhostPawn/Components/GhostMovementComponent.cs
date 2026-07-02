using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public sealed class GhostMovementComponent : GhostPawnComponent
    {
        private readonly Rigidbody _rb;

        private InputAction _moveAction;
        private Vector2 _moveDirection;
        private RigidbodyConstraints _defaultConstraints;

        public GhostMovementComponent(PlayerGhostPawn pawn, Rigidbody rb) : base(pawn)
        {
            _rb = rb;
        }

        public override void Initialize()
        {
            if (!_rb)
            {
                Logs.LogError("[GhostMovementComponent] Missing Rigidbody.", pawn);
                return;
            }

            if (!Settings)
            {
                Logs.LogError("[GhostMovementComponent] Missing GhostSettings.", pawn);
                return;
            }

            if (Owner == null || Owner.PlayerInput == null)
            {
                Logs.LogError("[GhostMovementComponent] Missing PlayerInput.", pawn);
                return;
            }

            _moveAction = Owner.PlayerInput.actions.FindAction("Move");

            if (_moveAction == null)
            {
                Logs.LogError("[GhostMovementComponent] Move action not found.", pawn);
                return;
            }

            _defaultConstraints = _rb.constraints;

            _rb.useGravity = false;
            _rb.isKinematic = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;

            LockHeightToGhostFloatHeight();
        }

        public override void OnEnterPawn()
        {
            _moveAction?.Enable();

            if (_rb)
            {
                _rb.useGravity = false;
                _rb.isKinematic = false;
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }

            _moveDirection = Vector2.zero;
            LockHeightToGhostFloatHeight();
        }

        public override void OnExitPawn()
        {
            _moveDirection = Vector2.zero;

            if (!_rb) return;
            
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.constraints = _defaultConstraints;
        }

        public override void FixedTick()
        {
            if (!pawn || !pawn.IsPawnActive || !_rb || !Settings)
                return;

            Vector2 input = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;

            if (input.sqrMagnitude < 0.01f)
                input = Vector2.zero;

            Vector2 targetDirection = input.normalized;
            Vector2 targetVelocity = targetDirection * Settings.MoveSpeed;

            float maxDelta = (
                targetVelocity.magnitude > _moveDirection.magnitude
                    ? Settings.Acceleration
                    : Settings.Deceleration
            ) * Time.fixedDeltaTime;

            _moveDirection = Vector2.MoveTowards(_moveDirection, targetVelocity, maxDelta);

            Vector3 currentVelocity = _rb.linearVelocity;

            Vector3 desiredVelocity = new(
                _moveDirection.x,
                currentVelocity.y,
                _moveDirection.y
            );

            Vector3 velocityChange = desiredVelocity - currentVelocity;
            velocityChange.y = 0f;

            _rb.AddForce(velocityChange, ForceMode.VelocityChange);

            RotateTowardsMovement();
        }

        public override void Reset()
        {
            _moveDirection = Vector2.zero;

            if (!_rb)
                return;

            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        private void RotateTowardsMovement()
        {
            Vector3 velocity3D = new(_moveDirection.x, 0f, _moveDirection.y);

            if (velocity3D.sqrMagnitude <= 0.001f)
                return;

            pawn.transform.rotation = Quaternion.Slerp(
                pawn.transform.rotation,
                Quaternion.LookRotation(velocity3D.normalized, Vector3.up),
                Time.fixedDeltaTime * 10f
            );
        }

        private void LockHeightToGhostFloatHeight()
        {
            if (!pawn || !Settings || !_rb)
                return;

            Vector3 position = pawn.transform.position;
            position.y += Settings.FloatHeight;

            pawn.transform.position = position;
            _rb.position = position;

            Physics.SyncTransforms();
        }

        public override void Dispose() => _moveAction = null;
    }
}