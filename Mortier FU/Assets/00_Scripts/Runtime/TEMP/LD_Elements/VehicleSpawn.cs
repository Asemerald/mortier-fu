using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

public class VehiculeSpawn : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VehicleModel[] _carPrototypes;
    [Space]
    [SerializeField] private Transform _startPoint;
    [SerializeField] private Transform _endPoint;
    [Space]
    [SerializeField] private GameObject _greenLight;
    [SerializeField] private GameObject _redLight;

    [Header("Parameters")]
    [SerializeField] private float _greenTimeDuration = 10f;
    [SerializeField] private float _redTimeDuration = 10f;
    [Space]
    [SerializeField] private float _vehicleSpawnCooldown = 2f;
    [SerializeField] private float _vehicleSpawnVariance = 0.3f;
    [Space]
    [SerializeField] private float _vehicleSpeed = 100f;
    
    //Commenté parce que pas utilisé et le warning me cassait les couilles
    //[SerializeField] private float _vehicleSpeedVariance = 0.2f;
    private bool _isRed;

    [Header("Pooling")]
    [SerializeField] private bool _collectionCheck = true;
    [SerializeField] private int _defaultCapacity = 10;
    [SerializeField] private int _maxSize = 1000;
    
    private ObjectPool<VehicleModel> _vehiclePool;
    private List<VehicleModel> _activeVehicles;
    private FrequencyTimer _vehicleSpawnTimer;
    private CountdownTimer _trafficLightTimer;
    private Transform _carParent;

    void Start()
    {
        _carParent = new GameObject("Cars").transform;
        _carParent.parent = transform;
        
        _vehiclePool = new ObjectPool<VehicleModel>(
            OnCreateVehicle,
            OnGetVehicle,
            OnReleaseVehicle,
            OnDestroyVehicle,
            _collectionCheck,
            _defaultCapacity,
            _maxSize
        );

        _activeVehicles = new List<VehicleModel>();

        _vehicleSpawnTimer = new FrequencyTimer(0f);
        _vehicleSpawnTimer.OnTick += SpawnVehicle;
        
        _trafficLightTimer = new CountdownTimer(0f);
        _trafficLightTimer.OnTimerStop += ToggleTrafficLight;
        
        _isRed = false;
        ToggleTrafficLight();
    }
    
    private void ToggleTrafficLight()
    {
        // Toggle
        _isRed ^= true;
        
        _redLight.SetActive(_isRed);
        _greenLight.SetActive(!_isRed);

        if (_isRed)
        {
            _vehicleSpawnTimer.Stop();
        }
        else
        {
            RandomizeSpawnCooldown();
            _vehicleSpawnTimer.Start();
        }
        
        _trafficLightTimer.Reset(_isRed ? _redTimeDuration : _greenTimeDuration);
        _trafficLightTimer.Start();
    }

    void SpawnVehicle()
    {
        RandomizeSpawnCooldown();
        _vehiclePool.Get();
    }

    void UpdateActiveCars()
    {
        for (int i = _activeVehicles.Count - 1; i >= 0; i--)
        {
            VehicleModel vehicle = _activeVehicles[i];
            Vector3 currentPosition = vehicle.Rigidbody.position;
            Vector3 endPosition = _endPoint.position;
            Vector3 newPosition = Vector3.MoveTowards(currentPosition, endPosition,
                                                      vehicle.Speed * Time.deltaTime);

            float sqrDist = (endPosition - newPosition).sqrMagnitude;
            if (sqrDist > 0.1)
            {
                vehicle.Rigidbody.MovePosition(newPosition);
            }
            else
            {
                DelayRelease(vehicle).Forget();
            }
        }
    }

    private async UniTaskVoid DelayRelease(VehicleModel vehicle)
    {
        _activeVehicles.Remove(vehicle);
        
        await UniTask.Delay(TimeSpan.FromSeconds(5f));

        if (vehicle)
        {
            _vehiclePool?.Release(vehicle);
        }
    }
    
    private void RandomizeSpawnCooldown()
    {
        float spawnCooldown = _vehicleSpawnCooldown * (1 + Random.Range(-_vehicleSpawnVariance, _vehicleSpawnVariance));
        _vehicleSpawnTimer.Reset(1f / spawnCooldown);
    }
    
    #region Pool Callbacks
    VehicleModel OnCreateVehicle()
    {
        if (_carPrototypes.Length <= 0)
        {
            Logs.LogWarning("No car prototype !");
            return null;
        }

        var vehicle = Instantiate(_carPrototypes[0], _carParent);
        if (!vehicle)
        {
            Logs.LogError("Failed instantiating a new car prototype !");
            return null;
        }

        vehicle.gameObject.SetActive(false);
        
        return vehicle;
    }

    void OnGetVehicle(VehicleModel vehicle)
    {
        vehicle.transform.position = _startPoint.position;
        vehicle.transform.rotation = Quaternion.LookRotation((_endPoint.position - _startPoint.position).normalized);
        
        vehicle.Speed = _vehicleSpeed * (1f + Random.Range(-_vehicleSpawnVariance, _vehicleSpawnVariance)) * 0.01f;

        var prototype = _carPrototypes[Random.Range(0, _carPrototypes.Length)];
        vehicle.ConfigureAsClone(prototype);
        
        vehicle.gameObject.SetActive(true);
        _activeVehicles.Add(vehicle);
    }

    void OnReleaseVehicle(VehicleModel vehicle)
    {
        vehicle.gameObject.SetActive(false);
    }

    void OnDestroyVehicle(VehicleModel vehicle)
    {
        if(vehicle)
            Destroy(vehicle.gameObject);
    }
    #endregion
    
    void FixedUpdate() => UpdateActiveCars();

    void OnDestroy()
    {
        _vehiclePool.Clear();
        _activeVehicles.Clear();
        
        _trafficLightTimer.OnTimerStop -= ToggleTrafficLight;
        _vehicleSpawnTimer.OnTick -= SpawnVehicle;

        _trafficLightTimer.Dispose();
        _vehicleSpawnTimer.Dispose();
    }
}
        