using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.Pool;

namespace MortierFu
{
    public class PuddleSystem : IGameSystem
    {
        public SO_PuddleSettings Settings { get; private set; }
        private GameObject _puddlePrefab;

        private PuddleFactory _puddleFactory;
        
        private IObjectPool<Puddle> _pool;
        private const bool k_collectionCheck = true;
        private const int k_defaultCapacity = 30;
        private const int k_maxSize = 10000;

        // Track active puddles
        private HashSet<Puddle> _active;

        private Transform _puddleParent;

        private const int k_maxImpactTargets = 50;
        
        public PuddleFactory PuddleFactory => _puddleFactory;

        public Puddle RequestPuddle(Puddle.Data puddleData)
        {
            Puddle puddle = _pool.Get();
            puddle.SetData(puddleData);
            puddle.OnGet();
            puddle.gameObject.SetActive(true);
            return puddle;
        }

        public void ReleasePuddle(Puddle puddle)
        {
            _pool.Release(puddle);
        }

        public void ClearActivePuddles()
        {
            if (_active.Count <= 0) return;

            // Release all active puddle, this remove them from hash set so must not foreach
            var activePuddles = new List<Puddle>(_active);
            for (int i = 0; i < activePuddles.Count; i++)
            {
                ReleasePuddle(activePuddles[i]);
            }
        }

        #region Puddle Callbacks

        private Puddle OnCreatePuddle()
        {
            var go = Object.Instantiate(_puddlePrefab, _puddleParent);
            var puddle = go.GetComponent<Puddle>();

            puddle.Initialize(this);
            go.SetActive(false);

            return puddle;
        }

        private void OnGetPuddle(Puddle puddle)
        {

            _active.Add(puddle);
        }

        private void OnReleasePuddle(Puddle puddle)
        {
            puddle.OnRelease();
            puddle.gameObject.SetActive(false);

            _active.Remove(puddle);
        }

        private void OnDestroyPuddle(Puddle puddle)
        {
            if (puddle && puddle.gameObject)
            {
                Object.Destroy(puddle.gameObject);
            }
        }

        #endregion

        public async UniTask OnInitialize()
        {
            // Load the system settings
            var settingsRef = SystemManager.Config.PuddleSettings;
            Settings = await AddressablesUtils.LazyLoadAsset(settingsRef);
            if (Settings == null) return;

            _puddlePrefab = await AddressablesUtils.LazyLoadAsset(Settings.PuddlePrefab);
            if (_puddlePrefab == null) return;

            _puddleParent = new GameObject("Puddles").transform;

            _active = new HashSet<Puddle>();
            _pool = new ObjectPool<Puddle>(
                OnCreatePuddle,
                OnGetPuddle,
                OnReleasePuddle,
                OnDestroyPuddle,
                k_collectionCheck,
                k_defaultCapacity,
                k_maxSize
            );
            
            _puddleFactory = new PuddleFactory(this);
        }

        public void Dispose()
        {
            _pool.Clear();
            _active.Clear();
        }

        public bool IsInitialized { get; set; }
    }
}