using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 5f;
        private Vector3 _moveDirection;
        private PlayerInput _playerInput;

        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _playerInput = GetComponent<PlayerInput>();
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
            float horizontal = _playerInput.actions["Move"].ReadValue<Vector2>().x;
            float vertical = _playerInput.actions["Move"].ReadValue<Vector2>().y;

            _moveDirection = new Vector2(horizontal, vertical).normalized * _moveSpeed;

            Vector3 lookDir = new Vector3(horizontal, 0f, vertical);
            
            if (lookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
            }
        }

        private void FixedUpdate()
        {
            Vector3 velocity = new Vector3(_moveDirection.x, _rb.linearVelocity.y, _moveDirection.y);
            _rb.linearVelocity = velocity;
        }
    }
}