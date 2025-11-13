using System;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEditor;
using UnityEngine;
using MathUtils = MortierFu.Shared.MathUtils;

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
            public float Scale;
            // public float Speed;
            public float Height;
            public float TravelTime;
            public float GravityScale;
        
            // Damage
            public int Damage;
            public float AoeRange;
            public int Bounces;
        }
        
        // TODO: Temporary to see the curves, can be extracted as a subcomponent ? Maybe it has to be swapped based on augments
        [SerializeField] private TrailRenderer _trail;
        [SerializeField] private ParticleSystem _smokeParticles;

        private Data _data;

        private float _t;
        private float _initialSpeed;
        private Vector3 _direction;
        private Vector3 _velocity;
        private float _angle;
        private float _travelTime;
        private float _timeFactor;
        private CountdownTimer _impactDebounceTimer;

        private BombshellSystem _system;
        private Rigidbody _rb;
        private Collider _col;

        public PlayerCharacter Owner => _data.Owner;
        public int Damage => _data.Damage;
        public float AoeRange => _data.AoeRange;
        
        public void Initialize(BombshellSystem system)
        {
            _system = system;
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<Collider>();
            _impactDebounceTimer = new CountdownTimer(0.1f);
        }
        
        public void SetData(Data data)
        {
            _data = data;
            _t = 0.0f;
            _impactDebounceTimer.Stop();

            Vector3 toTarget = _data.TargetPos - _data.StartPos;
            Vector3 groundDir = toTarget.With(y: 0f);
            Vector3 yTargetPos =  new Vector3(groundDir.magnitude, toTarget.y, 0);
            _direction = groundDir.normalized;
            ComputePathWithHeight(yTargetPos, _data.Height, _data.GravityScale, out _initialSpeed, out _angle, out _travelTime);
            _timeFactor = _travelTime / _data.TravelTime;

            transform.position = _data.StartPos;
            transform.rotation = Quaternion.LookRotation(_direction, Vector3.up);
            transform.localScale = Vector3.one * _data.Scale;
            
            HandleImpactAreaVFX().Forget();
        }

        public void ReturnToPool()
        {
            _system.ReleaseBombshell(this);
        }

        public void OnGet()
        {
            _smokeParticles.transform.SetParent(transform);
            _smokeParticles.Play();
            
            // TODO: This still causes some artifacts of the travel from old to new position
            _trail.Clear();
        }
        
        public void OnRelease()
        {
            _smokeParticles.transform.SetParent(null);
            _smokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        
        void FixedUpdate() {
            //_t += Time.deltaTime * _data.Speed * 0.1f;
            _t += Time.deltaTime * _timeFactor;

            Vector3 newPos = ComputePositionAtTime(_data.StartPos, _direction, _angle, _initialSpeed, _data.GravityScale, _t);
            _velocity = (newPos - _rb.position) / Time.deltaTime;

            if (_velocity.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(_velocity.normalized, Vector3.up);
                _rb.MoveRotation(targetRot);
            }
            
            _rb.MovePosition(newPos);
        }

        void OnTriggerEnter(Collider other) 
        {
            if (_impactDebounceTimer.IsRunning) return;
            
            // Cost-efficient, could cause problem in a later stage
            if (_system.Settings.DisableBombshellSelfCollision && other.TryGetComponent(out Bombshell bombshell) && bombshell.Owner == Owner)
                return;

            _impactDebounceTimer.Start();
            
            // Notify impact to the system
            _system.NotifyImpact(this);
            
            // Handle bounces
            if (_data.Bounces > 0) {
                _data.Bounces--;

                // Resolve collision
                bool overlapped = Physics.ComputePenetration(_col, transform.position, transform.rotation, other,
                    other.transform.position, other.transform.rotation, out var dir, out var distance);
                
                if (overlapped) {
                    _rb.MovePosition(_rb.position + dir * distance);
                }
                
                _data.StartPos = transform.position;
                // 1. Either treat initial speed or travel time, first one makes loss of height !
                // _initialSpeed *= 0.9f; // Loose 10% of speed
                // OR
                _data.TravelTime *= 0.9f;
                _timeFactor = _travelTime / _data.TravelTime;
                _t = 0f;
                
            } else {
                ReturnToPool();
            }
        }
        
        private async UniTask HandleImpactAreaVFX()
        {
            float delay = Mathf.Max(0f, _travelTime / _timeFactor - 0.6f);
            await UniTask.Delay(TimeSpan.FromSeconds(delay));
            
            if (TEMP_FXHandler.Instance)
            {
                TEMP_FXHandler.Instance.InstantiatePreview(_data.TargetPos, 0.6f, _data.AoeRange);
            }
            else Logs.LogWarning("No FX Handler");
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

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_data.TargetPos, 0.3f);
        }
    }
}