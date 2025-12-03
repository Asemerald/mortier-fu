using System;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
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
        
        [SerializeField] private TrailRenderer[] _trails;
        [SerializeField] private ParticleSystem _smokeParticles;

        private Data _data;

        private float _t;
        private float _initialSpeed;
        private Vector3 _direction;
        private Vector3 _velocity;
        private float _angle;
        private float _travelTime;
        private float _timeFactor;
        private float _resolvedTravelTime;
        private Vector3 _yTargetPos;
        private CountdownTimer _impactDebounceTimer;

        private BombshellSystem _system;
        private Rigidbody _rb;
        private Collider _col;

        public PlayerCharacter Owner => _data.Owner;
        
        #region API // TODO: This can be improved to faciliate alteration of Bombshell behaviours in augments and mods
        public int Damage
        {
            get => _data.Damage;
            set => _data.Damage = value;
        }
        public float AoeRange
        {
            get => _data.AoeRange;
            set => _data.AoeRange = value;
        }
        public int Bounces
        {
            get => _data.Bounces;
            set => _data.Bounces = value;
        }
        #endregion
        
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
            _direction = groundDir.normalized;
            _yTargetPos = new Vector3(groundDir.magnitude, toTarget.y, 0);
            
            ComputePathWithHeight(_yTargetPos, _data.Height, _data.GravityScale, out _initialSpeed, out _angle, out _travelTime);
            _timeFactor = _travelTime / _data.TravelTime;
            _resolvedTravelTime = _travelTime / _timeFactor;
            
            transform.position = _data.StartPos;
            transform.rotation = Quaternion.LookRotation(_direction, Vector3.up);
            transform.localScale = Vector3.one * _data.Scale;

            foreach (var trail in _trails)
            {
                trail.Clear();
                trail.emitting = true;
            }
            
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

            // TODO: temporary hack for trail renderer but still problem with the bombshell position on spawn
            foreach (var trail in _trails)
            {
                trail.Clear();
                trail.emitting = false;
            }
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

                float previousY = _data.TargetPos.y;
                _data.TargetPos += _data.TargetPos - _data.StartPos;
                _data.TargetPos.y = previousY; // Keep the same height target
                _data.StartPos = transform.position;
                
                _t = 0f;
                
                HandleImpactAreaVFX().Forget();
                
            } else {
                ReturnToPool();
            }
        }
        
        private async UniTask HandleImpactAreaVFX()
        {
            float delay = _resolvedTravelTime * _system.Settings.ImpactPreviewDelayFactor;
            await UniTask.Delay(TimeSpan.FromSeconds(delay));
            
            if (TEMP_FXHandler.Instance)
            {
                TEMP_FXHandler.Instance.InstantiatePreview(_data.TargetPos, _resolvedTravelTime - delay, _data.AoeRange);
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
    }
}