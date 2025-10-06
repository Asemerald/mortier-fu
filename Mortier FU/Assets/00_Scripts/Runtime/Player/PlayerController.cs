using UnityEngine;

namespace MortierFu
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 5f;
        private Vector3 _moveDirection;

        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            _moveDirection = new Vector3(horizontal, 0f, vertical).normalized * _moveSpeed;
        }

        private void FixedUpdate()
        {
            Vector3 velocity = new Vector3(_moveDirection.x, _rb.linearVelocity.y, _moveDirection.z);
            _rb.linearVelocity = velocity;
            //Debug.Log(_rb.linearVelocity);
        }
    }
}