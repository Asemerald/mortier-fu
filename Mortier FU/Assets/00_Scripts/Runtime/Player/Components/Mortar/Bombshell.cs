using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    [RequireComponent(typeof(Rigidbody))]
    public class Bombshell : MonoBehaviour
    {
        // Could be packed in an initialization struct
        private Character _owner;
        private float _damage;
        private float _radius;
        private float _speed;
        private float _impactRange;
        private float _gravityScale;
        private Vector3 _startPos;
        private Vector3 _targetPos;
        private Vector3 _initialVelocity;
        private float _t = -1.0f;

        private BombshellManager _manager;
        private Rigidbody _rb;

        public Character Owner => _owner;
        public float Damage => _damage;
        public float Radius => _radius;
        
        public void Initialize(BombshellManager manager, Character owner, float damage, float radius, float speed, float gravityScale, Vector3 start, Vector3 target)
        {
            // Already initialized
            if (_t >= 0.0f)
            {
                Logs.Warning("Trying to re-initialize a Bombshell.");
                return;
            }

            _manager = manager;
            _rb = GetComponent<Rigidbody>();

            _t = 0.0f;
            _owner = owner;
            _damage = damage;
            _radius = radius;
            _speed = speed;
            _gravityScale = gravityScale;
            _startPos = start;
            _targetPos = target;
            
            float targetT = Vector3.Distance(start, target) / _speed;
            _initialVelocity = InitialVelocityForTime(start, target, targetT, Physics.gravity * _gravityScale);
        }
        
        void Update()
        {
            _t += Time.deltaTime;
            
            Vector3 newPos = PositionAtTime(_startPos, _initialVelocity, _t, Physics.gravity * _gravityScale);
            _rb.MovePosition(newPos);
        }

        void OnTriggerEnter(Collider other)
        {
            // Notify impact & recycle the bombshell
            _manager.NotifyImpactAndRecycle(this);
        }
        
        static Vector3 InitialVelocityForTime(Vector3 start, Vector3 target, float T, Vector3 g)
        {
            if (T <= 0f) return Vector3.zero;
            return (target - start - 0.5f * g * T * T) / T;
        }

        static Vector3 PositionAtTime(Vector3 start, Vector3 v0, float t, Vector3 g)
        {
            return start + v0 * t + g * (0.5f * t * t);
        }
    }
}