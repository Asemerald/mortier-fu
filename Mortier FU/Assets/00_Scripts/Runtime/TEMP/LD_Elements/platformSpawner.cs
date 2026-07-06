using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace MortierFu
{
    public class PlatformSpawner : MonoBehaviour
    {
        private sealed class PlatformPoolEntry
        {
            public Movable Prefab;
            public ObjectPool<Movable> Pool;
        }

        [Header("Spawn")]
        [FormerlySerializedAs("platform")]
        [SerializeField] private List<GameObject> _platformPrefabs = new();

        [FormerlySerializedAs("targetPoint")]
        [SerializeField] private Transform _targetPoint;

        [FormerlySerializedAs("spawnTimeDelay")]
        [SerializeField] private Vector2 _spawnDelayRange = new(1f, 3f);

        [FormerlySerializedAs("platformSpeed")]
        [SerializeField] private float _platformSpeed = 5f;

        [Header("Pool")]
        [SerializeField] private int _prewarmCountPerPrefab = 2;
        [SerializeField] private int _defaultPoolCapacity = 8;
        [SerializeField] private int _maxPoolSize = 32;
        [SerializeField] private bool _collectionCheck = false;

        [Header("Timing")]
        [SerializeField] private bool _spawnImmediately = true;

        private readonly List<PlatformPoolEntry> _poolEntries = new();
        private readonly Dictionary<Movable, ObjectPool<Movable>> _poolsByInstance = new();
        private readonly List<Movable> _prewarmBuffer = new();

        private float _spawnTimer;
        private bool _isInitialized;

        private void Awake()
        {
            InitializePools();
        }

        private void OnEnable()
        {
            _spawnTimer = _spawnImmediately ? 0f : GetRandomSpawnDelay();
        }

        private void Update()
        {
            if (!_isInitialized)
                return;

            _spawnTimer -= Time.deltaTime;

            if (_spawnTimer > 0f)
                return;

            SpawnPlatform();
            _spawnTimer = GetRandomSpawnDelay();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _poolEntries.Count; i++)
                _poolEntries[i].Pool?.Clear();

            _poolEntries.Clear();
            _poolsByInstance.Clear();
            _prewarmBuffer.Clear();
        }

        private void InitializePools()
        {
            _poolEntries.Clear();
            _poolsByInstance.Clear();

            if (_platformPrefabs == null || _platformPrefabs.Count == 0)
            {
                Logs.LogWarning("[PlatformSpawner] No platform prefab assigned.", this);
                return;
            }

            for (int i = 0; i < _platformPrefabs.Count; i++)
            {
                GameObject prefabObject = _platformPrefabs[i];

                if (!prefabObject)
                {
                    Logs.LogWarning("[PlatformSpawner] Null platform prefab in list.", this);
                    continue;
                }

                if (!prefabObject.TryGetComponent(out Movable movablePrefab))
                {
                    Logs.LogWarning("[PlatformSpawner] Platform prefab has no Movable component.", prefabObject);
                    continue;
                }

                PlatformPoolEntry entry = new()
                {
                    Prefab = movablePrefab
                };

                entry.Pool = new ObjectPool<Movable>(
                    () => CreatePlatform(entry),
                    OnGetPlatform,
                    OnReleasePlatform,
                    OnDestroyPlatform,
                    _collectionCheck,
                    _defaultPoolCapacity,
                    _maxPoolSize
                );

                _poolEntries.Add(entry);

                PrewarmPool(entry.Pool, _prewarmCountPerPrefab);
            }

            _isInitialized = _poolEntries.Count > 0;
        }

        private Movable CreatePlatform(PlatformPoolEntry entry)
        {
            Movable instance = Instantiate(entry.Prefab, transform);
            instance.gameObject.SetActive(false);
            instance.SetReleaseCallback(ReleasePlatform);

            _poolsByInstance[instance] = entry.Pool;

            return instance;
        }

        private void PrewarmPool(ObjectPool<Movable> pool, int count)
        {
            if (pool == null || count <= 0)
                return;

            _prewarmBuffer.Clear();

            for (int i = 0; i < count; i++)
                _prewarmBuffer.Add(pool.Get());

            for (int i = 0; i < _prewarmBuffer.Count; i++)
                pool.Release(_prewarmBuffer[i]);

            _prewarmBuffer.Clear();
        }

        private void SpawnPlatform()
        {
            if (!_targetPoint)
            {
                Logs.LogWarning("[PlatformSpawner] No target point assigned.", this);
                return;
            }

            PlatformPoolEntry entry = GetRandomPoolEntry();

            if (entry?.Pool == null)
                return;

            Movable platform = entry.Pool.Get();

            platform.transform.SetParent(transform);
            platform.transform.SetPositionAndRotation(transform.position, transform.rotation);
            platform.Configure(_targetPoint, _platformSpeed);
        }

        private PlatformPoolEntry GetRandomPoolEntry() => _poolEntries.Count == 0 ? null : _poolEntries[Random.Range(0, _poolEntries.Count)];

        private void ReleasePlatform(Movable platform)
        {
            if (!platform)
                return;

            if (!_poolsByInstance.TryGetValue(platform, out ObjectPool<Movable> pool))
            {
                Destroy(platform.gameObject);
                return;
            }

            pool.Release(platform);
        }

        private void OnGetPlatform(Movable platform) => platform.gameObject.SetActive(true);

        private void OnReleasePlatform(Movable platform)
        {
            platform.ResetForPool();
            platform.transform.SetParent(transform);
            platform.gameObject.SetActive(false);
        }

        private void OnDestroyPlatform(Movable platform)
        {
            if (!platform)
                return;

            _poolsByInstance.Remove(platform);
            Destroy(platform.gameObject);
        }

        private float GetRandomSpawnDelay()
        {
            return Random.Range(_spawnDelayRange.x, _spawnDelayRange.y);
        }

        private void OnValidate()
        {
            if (_spawnDelayRange.x < 0f)
                _spawnDelayRange.x = 0f;

            if (_spawnDelayRange.y < _spawnDelayRange.x)
                _spawnDelayRange.y = _spawnDelayRange.x;

            if (_platformSpeed < 0f)
                _platformSpeed = 0f;

            if (_prewarmCountPerPrefab < 0)
                _prewarmCountPerPrefab = 0;

            if (_defaultPoolCapacity < 1)
                _defaultPoolCapacity = 1;

            if (_maxPoolSize < _defaultPoolCapacity)
                _maxPoolSize = _defaultPoolCapacity;
        }
    }
}