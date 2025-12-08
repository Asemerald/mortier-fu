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

        private Vector3 _direction;
        private Vector3 _velocity;
        private Vector3 _toTarget;
        private float _travelTime;
        private float _timeFactor;
        private float _resolvedTravelTime;
        
        private BombshellSystem _system;
        private Rigidbody _rb;
        
        private CountdownTimer _impactDebounceTimer;

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
            _impactDebounceTimer = new CountdownTimer(0.1f);
        }
        
        public void Configure(Data data)
        {
            // Initial setup
            _data = data;

            _toTarget = _data.TargetPos - _data.StartPos;
            CalculateTrajectory();
            
            // Set the scale for the initial scale.
            transform.localScale = Vector3.one * _data.Scale;
            
            _impactDebounceTimer.Stop();
            
            foreach (var trail in _trails)
            {
                trail.Clear();
                trail.emitting = true;
            }
            
            // Preview
            HandleImpactPreview().Forget();
        }
        
        private void CalculateTrajectory()
        {
            // Calculate horizontal direction and adjusted target position
            Vector3 groundDir = _toTarget.With(y: 0f);
            _direction = groundDir.normalized;
            var yTargetPos = new Vector3(groundDir.magnitude, _toTarget.y, 0);
            
            // Calculate the trajectory
            ComputePathWithHeight(yTargetPos, _system.Settings.BombshellHeight, _data.GravityScale, out float initialSpeed, out float angle, out _travelTime);
            _velocity = ComputeVelocityAtTime(_direction, angle, initialSpeed, _data.GravityScale, 0f);
            
            // Apply time modifications
            _timeFactor = _travelTime / _data.TravelTime;
            _resolvedTravelTime = _travelTime / _timeFactor;
            
            // Place the projectile according to the computed trajectory
            transform.position = _data.StartPos;
            transform.rotation = Quaternion.LookRotation(_direction, Vector3.up);
        }

        public void OnGet()
        {
            _smokeParticles.transform.SetParent(transform);
            _smokeParticles.Play();

            foreach (var trail in _trails)
            {
                trail.Clear();
                trail.emitting = true;
            }
        }

        public void OnRelease()
        {
            _smokeParticles.transform.SetParent(null);
            _smokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            foreach (var trail in _trails)
            {
                trail.emitting = false;
            }
        }
        
        public void ReturnToPool() => _system.ReleaseBombshell(this);
        
        // Move FixedUpdate to be called by the system (requires to be injected in the player loop or to be tailored to a MB)
        void FixedUpdate() {
			float dT = Time.deltaTime * _timeFactor;

            // Apply gravity
            _velocity += Physics.gravity * (_data.GravityScale * dT);
            
            // Compute intended movement
            Vector3 startPos = _rb.position;
            Vector3 moveDir = _velocity.normalized;
            float remainingDistance = _velocity.magnitude * dT;
            float radius = _data.Scale * 0.5f;
            const float k_skin = 0.01f;

            int safety = 0;
            while (remainingDistance > 0f && safety++ < 5)
            { 
                RaycastHit hit;
                if (Physics.SphereCast(startPos, radius, moveDir, out hit,
                                       remainingDistance, _system.Settings.WhatIsCollidable,
                                       QueryTriggerInteraction.Collide))
                {
                    // Move center to the point where the sphere center would be at impact
                    Vector3 centerAtHit = startPos + moveDir * hit.distance;
                    
                    // Nudge slightly away along the normal to avoid sticking
                    centerAtHit += hit.normal * k_skin;
                    
                    // Commit position
                    _rb.MovePosition(centerAtHit);
                    
                    // Notify impact
                    _system.NotifyImpact(this);

                    if (_data.Bounces > 0)
                    {
                        _data.Bounces--;
                        
                        // Reflect velocity using the collider normal
                        _velocity = Vector3.Reflect(_velocity, hit.normal) * _system.Settings.BounceDampingFactor;

                        // Recompute movement for the remaining distance after the hit:
                        // Subtract the distance we already travelled to the hit point.
                        remainingDistance -= hit.distance;

                        // Update startPos and moveDir for the next loop iteration
                        startPos = centerAtHit;
                        moveDir = (_velocity.sqrMagnitude > 0f) ? _velocity.normalized : Vector3.zero;
                        
                        // If velocity dropped to near zero, break
                        if (_velocity.sqrMagnitude < 0.0001f) break;
                    }
                    else
                    {
                        ReturnToPool();
                        return;
                    }
                }
                else
                {
                    // No collision: move the remaining distance and exit loop
                    Vector3 finalPos = startPos + moveDir * remainingDistance;
                    _rb.MovePosition(finalPos);
                    remainingDistance = 0f;
                }
            }
            
            // rotation toward velocity
            if (_velocity.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(_velocity.normalized, Vector3.up);
                _rb.MoveRotation(targetRot);
            }
        }
        
        private async UniTask HandleImpactPreview()
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
        
        private static Vector3 ComputeVelocityAtTime(Vector3 dir, float angle, float v0, float gravityScale, float t)
        {
            float g = -Physics.gravity.y * gravityScale;
    
            float vx = v0 * Mathf.Cos(angle);
            float vy = v0 * Mathf.Sin(angle) - g * t;

            return dir * vx + Vector3.up * vy;
        }
    }
}