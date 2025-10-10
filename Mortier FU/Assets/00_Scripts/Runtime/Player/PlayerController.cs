using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public class PlayerController : MonoBehaviour
    {
        private Character character;
        private Vector3 _moveDirection;
        private PlayerInput _playerInput;

        private Rigidbody _rb;

        public DA_CharacterStats CharacterStats { get; private set; }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _playerInput = GetComponent<PlayerInput>();
        }

        void Start()
        {
            if (!TryGetComponent(out character))
            {
                Logs.LogError("PlayerController requires a Character component on the same GameObject.");
                return;
            }
            CharacterStats = character.CharacterStats;
            transform.localScale = Vector3.one * CharacterStats.AvatarSize.Value;
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

            _moveDirection = new Vector2(horizontal, vertical).normalized * CharacterStats.MoveSpeed.Value;

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