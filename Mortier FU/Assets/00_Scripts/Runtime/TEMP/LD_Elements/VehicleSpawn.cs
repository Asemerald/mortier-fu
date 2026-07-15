using System.Collections.Generic;
using MortierFu.Shared;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

namespace MortierFu
{
    public class VehiculeSpawn : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private VehicleModel[] _carPrototypes;
        [SerializeField] private Transform _startPoint;
        [SerializeField] private Transform _endPoint;
        
        [SerializeField] private GameObject _greenLight;
        [SerializeField] private GameObject _redLight;

        [Header("Traffic Light")]
        [SerializeField] private bool _useTrafficLights = true;
        [ShowIf("_useTrafficLights")][SerializeField] private bool _startWithRedLight = true;
        [ShowIf("_useTrafficLights")][SerializeField] private float _greenTimeDuration = 10f;
        [ShowIf("_useTrafficLights")][SerializeField] private float _redTimeDuration = 4.5f;
        

        [Header("Spawn")]
        [SerializeField] private float _vehicleSpawnCooldown = 0.9f;
        [SerializeField] private float _vehicleSpawnVariance = 0.5f;

        [Header("Movement")]
        [SerializeField] private float _vehicleSpeed = 2200f;
        [SerializeField] private float _vehicleSpeedVariance = 0.02f;
        [SerializeField] private float _vehicleSpeedScale = 0.01f;
        [SerializeField] private float _minimumVehicleGap = 1.5f;
        [SerializeField] private float _arrivalDistance = 0.15f;

        [Header("Pooling")]
        [SerializeField] private int _prewarmCount = 10;
        [SerializeField] private bool _collectionCheck = false;
        [SerializeField] private int _defaultCapacity = 10;
        [SerializeField] private int _maxSize = 64;

        private readonly List<VehicleModel> _activeVehicles = new();
        private readonly List<VehicleModel> _validPrototypes = new();
        private readonly List<VehicleModel> _prewarmBuffer = new();

        private ObjectPool<VehicleModel> _vehiclePool;
        private VehicleModel _basePrototype;
        private Transform _carParent;

        private Vector3 _routeDirection;
        private Quaternion _routeRotation;
        private float _routeLength;

        private float _vehicleSpawnTimer;
        private float _trafficLightTimer;
        private bool _isRed;
        private bool _isInitialized;

        private void Awake()
        {
            InitializeRoute();
            InitializePrototypes();
            InitializePool();
        }

        private void Start()
        {
            if (!_isInitialized)
                return;
            if (_useTrafficLights)
            {
                SetTrafficLight(_startWithRedLight);
            }
            
        }

        private void Update()
        {
            if (!_isInitialized)
                return;
            if (_useTrafficLights)
            {
                UpdateTrafficLight();
            }
            if (_isRed)
                return;

            UpdateSpawnTimer();
        }

        private void FixedUpdate()
        {
            if (!_isInitialized)
                return;

            UpdateActiveVehicles();
        }

        private void OnDestroy()
        {
            _vehiclePool?.Clear();

            _activeVehicles.Clear();
            _validPrototypes.Clear();
            _prewarmBuffer.Clear();
        }

        private void InitializeRoute()
        {
            if (!_startPoint || !_endPoint)
            {
                Logs.LogError("[VehiculeSpawn] Missing start or end point.", this);
                return;
            }

            Vector3 route = _endPoint.position - _startPoint.position;
            _routeLength = route.magnitude;

            if (_routeLength <= 0.001f)
            {
                Logs.LogError("[VehiculeSpawn] Start point and end point are too close.", this);
                return;
            }

            _routeDirection = route / _routeLength;
            _routeRotation = Quaternion.LookRotation(_routeDirection, Vector3.up);
        }

        private void InitializePrototypes()
        {
            _validPrototypes.Clear();

            if (_carPrototypes == null || _carPrototypes.Length == 0)
            {
                Logs.LogError("[VehiculeSpawn] No car prototype assigned.", this);
                return;
            }

            for (int i = 0; i < _carPrototypes.Length; i++)
            {
                VehicleModel prototype = _carPrototypes[i];

                if (!prototype)
                {
                    Logs.LogWarning("[VehiculeSpawn] Null vehicle prototype in list.", this);
                    continue;
                }

                _validPrototypes.Add(prototype);
            }

            if (_validPrototypes.Count == 0)
            {
                Logs.LogError("[VehiculeSpawn] No valid vehicle prototype found.", this);
                return;
            }

            _basePrototype = _validPrototypes[0];
        }

        private void InitializePool()
        {
            if (!_startPoint || !_endPoint || !_basePrototype)
                return;

            _carParent = new GameObject("Cars").transform;
            _carParent.SetParent(transform);

            _vehiclePool = new ObjectPool<VehicleModel>(
                OnCreateVehicle,
                OnGetVehicle,
                OnReleaseVehicle,
                OnDestroyVehicle,
                _collectionCheck,
                _defaultCapacity,
                _maxSize
            );

            PrewarmPool();

            _isInitialized = true;
        }

        private void PrewarmPool()
        {
            if (_prewarmCount <= 0)
                return;

            _prewarmBuffer.Clear();

            for (int i = 0; i < _prewarmCount; i++)
                _prewarmBuffer.Add(_vehiclePool.Get());

            for (int i = 0; i < _prewarmBuffer.Count; i++)
                _vehiclePool.Release(_prewarmBuffer[i]);

            _prewarmBuffer.Clear();
        }

        private void UpdateTrafficLight()
        {
            _trafficLightTimer -= Time.deltaTime;

            if (_trafficLightTimer > 0f)
                return;

            SetTrafficLight(!_isRed);
        }

        private void SetTrafficLight(bool red)
        {
            _isRed = red;

            if (_redLight)
                _redLight.SetActive(_isRed);

            if (_greenLight)
                _greenLight.SetActive(!_isRed);

            _trafficLightTimer = _isRed ? _redTimeDuration : _greenTimeDuration;

            if (!_isRed)
                ResetVehicleSpawnTimer();
        }

        private void UpdateSpawnTimer()
        {
            _vehicleSpawnTimer -= Time.deltaTime;

            if (_vehicleSpawnTimer > 0f)
                return;

            TrySpawnVehicle();
            ResetVehicleSpawnTimer();
        }

        private void ResetVehicleSpawnTimer()
        {
            float variance = Random.Range(-_vehicleSpawnVariance, _vehicleSpawnVariance);
            _vehicleSpawnTimer = Mathf.Max(0.05f, _vehicleSpawnCooldown * (1f + variance));
        }

        private void TrySpawnVehicle()
        {
            VehicleModel prototype = GetRandomPrototype();

            if (!prototype)
                return;

            VehicleModel vehicle = _vehiclePool.Get();

            vehicle.ConfigureAsClone(prototype);
            vehicle.Progress = 0f;
            vehicle.Speed = GetRandomVehicleSpeed();

            if (!HasSpawnRoom(vehicle))
            {
                _vehiclePool.Release(vehicle);
                return;
            }

            vehicle.transform.SetPositionAndRotation(_startPoint.position, _routeRotation);
            vehicle.gameObject.SetActive(true);

            _activeVehicles.Add(vehicle);
        }

        private bool HasSpawnRoom(VehicleModel candidate)
        {
            if (_activeVehicles.Count == 0)
                return true;

            VehicleModel lastVehicle = _activeVehicles[^1];

            if (!lastVehicle)
                return true;

            float requiredGap = GetRequiredGap(candidate, lastVehicle);
            return lastVehicle.Progress >= requiredGap;
        }

        private float GetRandomVehicleSpeed()
        {
            float variance = Random.Range(-_vehicleSpeedVariance, _vehicleSpeedVariance);
            return Mathf.Max(0f, _vehicleSpeed * (1f + variance) * _vehicleSpeedScale);
        }

        private VehicleModel GetRandomPrototype() => _validPrototypes.Count == 0 ? null : _validPrototypes[Random.Range(0, _validPrototypes.Count)];

        private void UpdateActiveVehicles()
        {
            float deltaTime = Time.fixedDeltaTime;

            for (int i = 0; i < _activeVehicles.Count; i++)
            {
                VehicleModel vehicle = _activeVehicles[i];

                if (!vehicle)
                {
                    _activeVehicles.RemoveAt(i);
                    i--;
                    continue;
                }

                float nextProgress = vehicle.Progress + vehicle.Speed * deltaTime;

                if (i > 0)
                {
                    VehicleModel frontVehicle = _activeVehicles[i - 1];

                    if (frontVehicle)
                    {
                        float maxProgress = frontVehicle.Progress - GetRequiredGap(vehicle, frontVehicle);
                        nextProgress = Mathf.Min(nextProgress, maxProgress);
                    }
                }

                nextProgress = Mathf.Clamp(nextProgress, vehicle.Progress, _routeLength);

                vehicle.Progress = nextProgress;

                Vector3 nextPosition = _startPoint.position + _routeDirection * nextProgress;
                vehicle.Rigidbody.MovePosition(nextPosition);

                if (_routeLength - nextProgress > _arrivalDistance)
                    continue;

                ReleaseActiveVehicleAt(i);
                i--;
            }
        }

        private float GetRequiredGap(VehicleModel backVehicle, VehicleModel frontVehicle) => backVehicle.HalfLength + frontVehicle.HalfLength + _minimumVehicleGap;

        private void ReleaseActiveVehicleAt(int index)
        {
            VehicleModel vehicle = _activeVehicles[index];
            _activeVehicles.RemoveAt(index);

            if (vehicle)
                _vehiclePool.Release(vehicle);
        }

        private VehicleModel OnCreateVehicle()
        {
            VehicleModel vehicle = Instantiate(_basePrototype, _carParent);

            if (!vehicle)
            {
                Logs.LogError("[VehiculeSpawn] Failed instantiating a vehicle.", this);
                return null;
            }

            vehicle.gameObject.SetActive(false);
            return vehicle;
        }

        private void OnGetVehicle(VehicleModel vehicle)
        { }

        private void OnReleaseVehicle(VehicleModel vehicle)
        {
            if (!vehicle)
                return;

            vehicle.ResetRuntime();
            vehicle.gameObject.SetActive(false);
        }

        private void OnDestroyVehicle(VehicleModel vehicle)
        {
            if (vehicle)
                Destroy(vehicle.gameObject);
        }

        private void OnValidate()
        {
            if (_greenTimeDuration < 0.1f)
                _greenTimeDuration = 0.1f;

            if (_redTimeDuration < 0.1f)
                _redTimeDuration = 0.1f;

            if (_vehicleSpawnCooldown < 0.05f)
                _vehicleSpawnCooldown = 0.05f;

            if (_vehicleSpawnVariance < 0f)
                _vehicleSpawnVariance = 0f;

            if (_vehicleSpeed < 0f)
                _vehicleSpeed = 0f;

            if (_vehicleSpeedVariance < 0f)
                _vehicleSpeedVariance = 0f;

            if (_vehicleSpeedScale < 0f)
                _vehicleSpeedScale = 0f;

            if (_minimumVehicleGap < 0f)
                _minimumVehicleGap = 0f;

            if (_arrivalDistance < 0.01f)
                _arrivalDistance = 0.01f;

            if (_prewarmCount < 0)
                _prewarmCount = 0;

            if (_defaultCapacity < 1)
                _defaultCapacity = 1;

            if (_maxSize < _defaultCapacity)
                _maxSize = _defaultCapacity;
        }
    }
}