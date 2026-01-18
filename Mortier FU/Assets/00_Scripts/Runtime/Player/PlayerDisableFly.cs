using UnityEngine;

namespace MortierFu
{
    public class PlayerDisableFly : MonoBehaviour
    {
        [SerializeField] private Rigidbody _rigidbody;

        private void FixedUpdate()
        {
            var currentVelocity = _rigidbody.linearVelocity;
            currentVelocity.y = Mathf.Min(0, currentVelocity.y);
            
            _rigidbody.linearVelocity = new Vector3(_rigidbody.linearVelocity.x,
                currentVelocity.y,
                _rigidbody.linearVelocity.z);
        }
    }
}