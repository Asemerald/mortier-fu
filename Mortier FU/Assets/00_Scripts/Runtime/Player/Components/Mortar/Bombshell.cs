using System;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using MathUtils = MortierFu.Shared.MathUtils;
using Random = UnityEngine.Random;

namespace MortierFu
{
    [RequireComponent(typeof(Rigidbody), typeof(BombshellAspect))]
    public class Bombshell : MonoBehaviour
    {
        public struct Data
        {
            public PlayerCharacter Owner;

            public Vector3 StartPos;
            public Vector3 TargetPos;
            public float Scale;
            public float Speed;
            public float GravityScale;

            public float Damage;
            public float AoeRange;
            public int Bounces;
        }

        private Data _data;

        private Vector3 _direction;
        private Vector3 _velocity;
        private Vector3 _toTarget;
        private float _travelTime;
        private float _bounceRange;
        private int _previewRequestId;

        private BombshellSystem _system;
        private FXService _fxService;
        private Rigidbody _rb;
        private BombshellAspect _aspect;

        private CountdownTimer _impactDebounceTimer;

        public PlayerCharacter Owner => _data.Owner;

        public float Damage
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

        public void MultiplyScale(float scalar)
        {
            _data.Scale *= scalar;
            transform.localScale = Vector3.one * _data.Scale;
        }

        public float GetTravelTime() => _travelTime / _data.Speed;

        public void Initialize(BombshellSystem system)
        {
            _system = system;
            _fxService = ServiceManager.Instance.Get<FXService>();

            _rb = GetComponent<Rigidbody>();

            _aspect = GetComponent<BombshellAspect>();
            _aspect.Initialize(this);

            _impactDebounceTimer = new CountdownTimer(0.1f);
        }

        public void Configure(Data data)
        {
            _data = data;
            _previewRequestId++;

            transform.localScale = Vector3.one * _data.Scale;

            _toTarget = GetTrajectoryTargetPosition() - _data.StartPos;
            _bounceRange = Mathf.Max(0.1f, _toTarget.With(y: 0f).magnitude);

            CalculateTrajectory();

            _impactDebounceTimer.Stop();

            StartImpactPreviewFrom(_data.StartPos, _velocity);
        }

        private Vector3 GetTrajectoryTargetPosition()
        {
            float radius = _data.Scale * 0.5f;

            return _data.TargetPos + Vector3.up * radius;
        }

        private void CalculateTrajectory()
        {
            Vector3 groundDir = _toTarget.With(y: 0f);

            if (groundDir.sqrMagnitude < 0.0001f)
                groundDir = transform.forward.With(y: 0f);

            if (groundDir.sqrMagnitude < 0.0001f)
                groundDir = Vector3.forward;

            _direction = groundDir.normalized;

            Vector3 yTargetPos = new(groundDir.magnitude, _toTarget.y, 0f);

            float bombshellHeight = CalculateBombshellHeight(groundDir.magnitude);

            ComputePathWithHeight(
                yTargetPos,
                bombshellHeight,
                _data.GravityScale,
                out float initialSpeed,
                out float angle,
                out _travelTime
            );

            _velocity = ComputeVelocityAtTime(
                _direction,
                angle,
                initialSpeed,
                _data.GravityScale,
                0f
            );

            Quaternion rotation = Quaternion.LookRotation(_direction, Vector3.up);
            SetPositionAndRotationImmediate(_data.StartPos, rotation);
        }

        private float CalculateBombshellHeight(float targetDistance)
        {
            Vector2 distRange = _system.Settings.BombshellHeightDistance;
            Vector2 valueRange = _system.Settings.BombshellHeightValue;

            float alpha = Mathf.InverseLerp(distRange.x, distRange.y, targetDistance);
            float curvedAlpha = _system.Settings.BombshellHeightCurve.Evaluate(alpha);

            return Mathf.Lerp(valueRange.x, valueRange.y, curvedAlpha);
        }

        public void OnGet()
        {
            _aspect.OnGet();
        }

        public void OnRelease()
        {
            _previewRequestId++;
            _aspect.OnRelease();
        }

        public void ReturnToPool() => _system.ReleaseBombshell(this);

        private void FixedUpdate()
        {
            if (!_rb)
                return;

            float remainingSimulationTime = Time.fixedDeltaTime * _data.Speed;

            if (remainingSimulationTime <= 0f)
                return;

            Vector3 gravityAcceleration = Physics.gravity * _data.GravityScale;
            Vector3 currentPos = _rb.position;

            float radius = _data.Scale * 0.5f;
            const float k_skin = 0.01f;

            int safety = 0;

            while (remainingSimulationTime > 0f && safety++ < 5)
            {
                if (!TryComputeStep(
                        _velocity,
                        gravityAcceleration,
                        remainingSimulationTime,
                        out Vector3 displacement,
                        out Vector3 nextVelocity))
                {
                    break;
                }

                float moveDistance = displacement.magnitude;

                if (moveDistance <= 0.0001f)
                {
                    _velocity = nextVelocity;
                    remainingSimulationTime = 0f;
                    break;
                }

                Vector3 moveDir = displacement / moveDistance;

                if (Physics.SphereCast(
                        currentPos,
                        radius,
                        moveDir,
                        out RaycastHit hit,
                        moveDistance,
                        _system.Settings.WhatIsCollidable,
                        QueryTriggerInteraction.Collide))
                {
                    float stepRatio = Mathf.Clamp01(hit.distance / moveDistance);
                    float simulationTimeToHit = remainingSimulationTime * stepRatio;

                    Vector3 velocityAtHit = _velocity + gravityAcceleration * simulationTimeToHit;
                    Vector3 centerAtHit = currentPos + moveDir * hit.distance;
                    centerAtHit += hit.normal * k_skin;

                    SetPositionImmediate(centerAtHit);

                    _velocity = velocityAtHit;

                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Water"))
                    {
                        AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Mortar_Water, hit.point);
                        _fxService.PlayWaterExplosionFX(hit.point);
                        ReturnToPool();
                        return;
                    }

                    _system.NotifyImpact(this, hit);

                    if (_data.Bounces <= 0)
                    {
                        ReturnToPool();
                        return;
                    }

                    _data.Bounces--;

                    BounceContext bounceContext = new();
                    EventBus<TriggerBounce>.Raise(new TriggerBounce
                    {
                        Bombshell = this,
                        Context = bounceContext
                    });

                    ApplyBounceVelocity(bounceContext);

                    _data.Damage *= _system.Settings.BounceDamageDampingFactor;

                    currentPos = centerAtHit;

                    StartImpactPreviewFrom(centerAtHit, _velocity);

                    remainingSimulationTime = 0f;
                    break;
                }

                currentPos += displacement;
                _velocity = nextVelocity;

                SetPositionImmediate(currentPos);

                remainingSimulationTime = 0f;
            }

            UpdateRotationFromVelocity();
        }

        private static bool TryComputeStep(Vector3 velocity, Vector3 acceleration, float deltaTime, out Vector3 displacement, out Vector3 nextVelocity)
        {
            if (deltaTime <= 0f)
            {
                displacement = Vector3.zero;
                nextVelocity = velocity;
                return false;
            }

            nextVelocity = velocity + acceleration * deltaTime;
            displacement = velocity * deltaTime + acceleration * (0.5f * deltaTime * deltaTime);

            return true;
        }

        private void ApplyBounceVelocity(BounceContext bounceContext)
        {
            if (bounceContext != null && bounceContext.ForceInPlaceBounce)
            {
                ApplyInPlaceBounceVelocity();
                return;
            }

            Vector3 bounceDirection = GetFlatBounceDirection();

            if (bounceContext != null &&
                (bounceContext.UpRotationMinAngle != 0f || bounceContext.RotationMaxAngle != 0f))
            {
                float randomAngle = Random.Range(
                    bounceContext.UpRotationMinAngle,
                    bounceContext.RotationMaxAngle
                );

                bounceDirection = Quaternion.AngleAxis(randomAngle, Vector3.up) * bounceDirection;
            }

            _direction = bounceDirection.normalized;

            RecalculateVelocityForBounceRange(_direction, _bounceRange);
        }

        private Vector3 GetFlatBounceDirection()
        {
            Vector3 bounceDirection = _velocity.With(y: 0f);

            if (bounceDirection.sqrMagnitude < 0.0001f)
                bounceDirection = _direction;

            if (bounceDirection.sqrMagnitude < 0.0001f)
                bounceDirection = transform.forward.With(y: 0f);

            if (bounceDirection.sqrMagnitude < 0.0001f)
                bounceDirection = Vector3.forward;

            return bounceDirection.normalized;
        }

        private void ApplyInPlaceBounceVelocity()
        {
            float verticalSpeed = CalculateVerticalSpeedForRange(_bounceRange);

            _velocity = Vector3.up * verticalSpeed;

            UpdateTravelTimeForVerticalBounce(verticalSpeed);
        }

        private float CalculateVerticalSpeedForRange(float range)
        {
            float height = CalculateBombshellHeight(range);
            float gravity = -Physics.gravity.y * _data.GravityScale;

            if (gravity <= 0.0001f)
                return 1.75f;

            return Mathf.Sqrt(2f * gravity * Mathf.Max(0.1f, height));
        }

        private void RecalculateVelocityForBounceRange(Vector3 bounceDirection, float bounceRange)
        {
            bounceRange = Mathf.Max(0.1f, bounceRange);

            Vector3 targetPos = new(bounceRange, 0f, 0f);

            float bombshellHeight = CalculateBombshellHeight(bounceRange);

            ComputePathWithHeight(
                targetPos,
                bombshellHeight,
                _data.GravityScale,
                out float initialSpeed,
                out float angle,
                out _travelTime
            );

            _velocity = ComputeVelocityAtTime(
                bounceDirection,
                angle,
                initialSpeed,
                _data.GravityScale,
                0f
            );
        }

        private void UpdateTravelTimeForVerticalBounce(float verticalSpeed)
        {
            float gravity = -Physics.gravity.y * _data.GravityScale;

            if (gravity <= 0.0001f)
            {
                _travelTime = 0f;
                return;
            }

            _travelTime = verticalSpeed * 2f / gravity;
        }

        private void StartImpactPreviewFrom(Vector3 startPos, Vector3 startVelocity)
        {
            int requestId = ++_previewRequestId;

            HandleImpactPreviewAsync(
                requestId,
                startPos,
                startVelocity
            ).Forget();
        }

        private async UniTask HandleImpactPreviewAsync(int requestId, Vector3 startPos, Vector3 startVelocity)
        {
            if (!TryPredictNextImpact(
                    startPos,
                    startVelocity,
                    out RaycastHit hitResult,
                    out float realTimeToImpact,
                    out int simulationIterations))
            {
                return;
            }

            if (_system.Settings.EnableDebug)
            {
                Logs.Log(
                    $"[Bombshell Preview] Predicted impact on '{hitResult.collider.name}' " +
                    $"layer '{LayerMask.LayerToName(hitResult.collider.gameObject.layer)}' " +
                    $"at {hitResult.point}. Aim target was {_data.TargetPos}. " +
                    $"Iterations: {simulationIterations}."
                );
            }

            float delay = Mathf.Max(0f, realTimeToImpact * _system.Settings.ImpactPreviewDelayFactor);
            float previewDuration = Mathf.Max(0.01f, realTimeToImpact - delay);

            await UniTask.Delay(TimeSpan.FromSeconds(delay));

            if (requestId != _previewRequestId)
                return;

            if (!this || !gameObject.activeInHierarchy)
                return;

            if (_fxService != null)
            {
                _fxService.PlayBombshellPreview(
                    hitResult.point,
                    hitResult.normal,
                    previewDuration,
                    _data.AoeRange
                );
            }
            else
            {
                Logs.LogWarning("No FX Handler");
            }
        }

        private bool TryPredictNextImpact(Vector3 startPos, Vector3 startVelocity, out RaycastHit hitResult, out float realTimeToImpact, out int simulationIterations)
        {
            const float k_maxRealTime = 30f;

            hitResult = default;
            realTimeToImpact = 0f;
            simulationIterations = 0;

            Vector3 currentPos = startPos;
            Vector3 currentVelocity = startVelocity;
            Vector3 gravityAcceleration = Physics.gravity * _data.GravityScale;

            float radius = _data.Scale * 0.5f;

            while (realTimeToImpact < k_maxRealTime)
            {
                simulationIterations++;

                float realDeltaTime = Time.fixedDeltaTime;
                float simulationDeltaTime = realDeltaTime * _data.Speed;

                if (!TryComputeStep(
                        currentVelocity,
                        gravityAcceleration,
                        simulationDeltaTime,
                        out Vector3 displacement,
                        out Vector3 nextVelocity))
                {
                    return false;
                }

                float moveDistance = displacement.magnitude;

                if (moveDistance <= 0.0001f)
                {
                    currentVelocity = nextVelocity;
                    realTimeToImpact += realDeltaTime;
                    continue;
                }

                Vector3 moveDir = displacement / moveDistance;

                if (_system.Settings.EnableDebug)
                {
                    Debug.DrawLine(
                        currentPos,
                        currentPos + displacement,
                        Color.red,
                        4f
                    );
                }

                if (Physics.SphereCast(
                        currentPos,
                        radius,
                        moveDir,
                        out hitResult,
                        moveDistance,
                        _system.Settings.WhatIsCollidable,
                        QueryTriggerInteraction.Collide))
                {
                    float stepRatio = Mathf.Clamp01(hitResult.distance / moveDistance);
                    realTimeToImpact += realDeltaTime * stepRatio;
                    return true;
                }

                currentPos += displacement;
                currentVelocity = nextVelocity;
                realTimeToImpact += realDeltaTime;
            }

            return false;
        }

        private void SetPositionImmediate(Vector3 position)
        {
            transform.position = position;

            if (_rb)
                _rb.position = position;

            Physics.SyncTransforms();
        }

        private void SetPositionAndRotationImmediate(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);

            if (_rb)
            {
                _rb.position = position;
                _rb.rotation = rotation;
            }

            Physics.SyncTransforms();
        }

        private void SetRotationImmediate(Quaternion rotation)
        {
            transform.rotation = rotation;

            if (_rb)
                _rb.rotation = rotation;
        }

        private void UpdateRotationFromVelocity()
        {
            if (_velocity.sqrMagnitude <= 0.001f)
                return;

            Vector3 forward = _velocity.normalized;
            Vector3 up = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.98f
                ? Vector3.forward
                : Vector3.up;

            SetRotationImmediate(Quaternion.LookRotation(forward, up));
        }

        private static void ComputePathWithHeight(Vector3 targetPos, float height, float gravityScale, out float v0, out float angle, out float time)
        {
            float xt = Mathf.Max(0.001f, targetPos.x);
            float yt = targetPos.y;
            float g = -Physics.gravity.y * gravityScale;

            height = Mathf.Max(height, yt + 0.01f, 0.01f);

            float b = Mathf.Sqrt(2f * g * height);
            float a = -0.5f * g;
            float c = -yt;

            float tplus = MathUtils.QuadraticEquation(a, b, c, 1);
            float tmin = MathUtils.QuadraticEquation(a, b, c, -1);

            time = Mathf.Max(tplus, tmin, 0.001f);

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