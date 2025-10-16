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
            public Character Owner;
        
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
        
        // TODO: Make it configurable
        const float k_height = 10.0f;

        // Configurable bombshell properties
        private Data _data;
        
        // Movement tracking and curve calculation
        private float _elapsed;
        private float _initialSpeed;
        private Vector3 _direction;
        private float _angle;
        private float _travelTime;
        private float _timeFactor;

        // Dependencies
        private BombshellSystem _system;
        private Rigidbody _rb;

        // Getters
        public Character Owner => _data.Owner;
        public float Damage => _data.Damage;
        public float AoeRange => _data.AoeRange;

        /// Called once when the bombshell is instantiated.
        public void Initialize(BombshellSystem system)
        {
            _system = system;
            _rb = GetComponent<Rigidbody>();
        }

        /// Called each time the bombshell is reused.
        public void Configure(Data data)
        {
            _data = data;
            _elapsed = 0.0f;
            
            Vector3 toTarget = _data.TargetPos - _data.StartPos;
            Vector3 groundDir = toTarget.With(y: 0f);
            _data.TargetPos = new Vector3(groundDir.magnitude, toTarget.y, 0);
            _direction = groundDir.normalized;
            ComputePathWithHeight(_data.TargetPos, k_height, _data.GravityScale, out _initialSpeed, out _angle, out _travelTime);
            _timeFactor = _travelTime / _data.TravelTime;
        }

        public void Reset()
        {
            _data = default;

            _elapsed = 0f;
            _initialSpeed = 0f;
            _direction = Vector3.zero;
            _angle = 0f;
            _travelTime = 0f;
            _timeFactor = 1f;
        }
        
        void FixedUpdate()
        {
            //_t += Time.deltaTime * _data.Speed * 0.1f;
            _elapsed += Time.deltaTime * _timeFactor;

            Vector3 newPos = ComputePositionAtTime(_data.StartPos, _direction, _angle, _initialSpeed, _data.GravityScale, _elapsed);
            _rb.MovePosition(newPos);
        }

        void OnTriggerEnter(Collider other)
        {
            // Notify impact & recycle the bombshell
            _system.NotifyImpactAndRecycle(this);
        }
        
        #region Curve Maths
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
        #endregion
    }
}