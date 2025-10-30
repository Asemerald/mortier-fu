using System;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    [RequireComponent(typeof(Rigidbody))]
    public class Bombshell : MonoBehaviour
    {
        public struct Data
        {
            // Meta
            public PlayerCharacter Owner;
        
            // Movement
            public Vector3 StartPos;
            public Vector3 TargetPos;
            // public float Speed;
            public float TravelTime;
            public float GravityScale;
        
            // Damage
            public float Damage;
            public float AoeRange;
        }
        
        // TODO: Temporary to see the curves
        [SerializeField] private TrailRenderer _trail;

        private Data _data;
        
        private float _t = -1.0f;
        private float _initialSpeed;
        private Vector3 _direction;
        private float _angle;
        private float _travelTime;
        private float _timeFactor;

        private BombshellManager _manager;
        private Rigidbody _rb;

        public PlayerCharacter Owner => _data.Owner;
        public float Damage => _data.Damage;
        public float AoeRange => _data.AoeRange;

        public void Initialize(BombshellManager manager, Data data)
        {
            // Already initialized
            if (_t >= 0.0f)
            {
                Logs.LogWarning("Trying to re-initialize a Bombshell.");
                return;
            }

            _manager = manager;
            _rb = GetComponent<Rigidbody>();

            _data = data;
            _t = 0.0f;

            //Change k_height to be link with projectile travel time
            /*const*/ float k_height = _data.TravelTime * 8;
            Vector3 toTarget = _data.TargetPos - _data.StartPos;
            Vector3 groundDir = toTarget.With(y: 0f);
            _data.TargetPos = new Vector3(groundDir.magnitude, toTarget.y, 0);
            _direction = groundDir.normalized;
            ComputePathWithHeight(_data.TargetPos, k_height, _data.GravityScale, out _initialSpeed, out _angle, out _travelTime);
            _timeFactor = _travelTime / _data.TravelTime;
        }
        
        void Update()
        {
            //_t += Time.deltaTime * _data.Speed * 0.1f;
            _t += Time.deltaTime * _timeFactor;

            Vector3 newPos = ComputePositionAtTime(_data.StartPos, _direction, _angle, _initialSpeed, _data.GravityScale, _t);
            _rb.MovePosition(newPos);
        }

        void OnTriggerEnter(Collider other)
        {
            // Notify impact & recycle the bombshell
            _manager.NotifyImpactAndRecycle(this);
        }

        /// <summary>
        /// Calculates the initial velocity, launch angle, and flight time required to reach a target position
        /// with a specified arc height and gravity scale.
        /// </summary>
        /// <param name="targetPos">The target position to reach.</param>
        /// <param name="height">The desired maximum height of the arc.</param>
        /// <param name="gravityScale">The scale factor for gravity.</param>
        /// <param name="v0">Output: The required initial velocity.</param>
        /// <param name="angle">Output: The launch angle in radians.</param>
        /// <param name="time">Output: The total flight time.</param>
        private static void ComputePathWithHeight(Vector3 targetPos, float height, float gravityScale, 
            out float v0, out float angle, out float time)
        {
            float xt = targetPos.x;
            float yt = targetPos.y;
            float g = -Physics.gravity.y * gravityScale;
            
            float b = Mathf.Sqrt(2 * g * height);
            float a = -0.5f * g;
            float c = -yt;
            
            float tplus = MathUtils.QuadraticEquation(a, b, c, 1);
            float tmin = MathUtils.QuadraticEquation(a, b, c, -1);
            time = tplus > tmin ? tplus : tmin;

            angle = Mathf.Atan(b * time / xt);
 
            v0 = b / Mathf.Sin(angle);
        }
        
        private static Vector3 ComputePositionAtTime(Vector3 start, Vector3 dir, float angle, float v0,
            float gravityScale, float t)
        {
            float x = v0 * t * Mathf.Cos(angle);
            float y = v0 * t * Mathf.Sin(angle) - 0.5f * Physics.gravity.y * -gravityScale * t * t;

            return start + dir * x + Vector3.up * y;
        }

        // TODO: Remove this, trail view purpose only
        private void OnDestroy()
        {
            _trail.transform.SetParent(null);
            Destroy(_trail.gameObject, 0.6f);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_data.StartPos + _data.TargetPos, 0.2f);
        }
    }
}