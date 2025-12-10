using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine.Pool;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MortierFu
{
    public class PuddleSystem : IGameSystem
    {
        private AsyncOperationHandle<SO_PuddleSettings> _settingsHandle;
        public SO_PuddleSettings Settings => _settingsHandle.Result;
        private AsyncOperationHandle<GameObject> _puddlePrefabHandle;

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
            var puddleGo = Object.Instantiate(_puddlePrefabHandle.Result, _puddleParent);
            var puddle = puddleGo.GetComponent<Puddle>();

            puddle.Initialize(this);
            puddleGo.SetActive(false);

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
            _settingsHandle = await SystemManager.Config.PuddleSettings.LazyLoadAssetRef();
            _puddlePrefabHandle = await Settings.PuddlePrefab.LazyLoadAssetRef();

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
            
            Addressables.Release(_puddlePrefabHandle);
            Addressables.Release(_settingsHandle);
        }

        public bool IsInitialized { get; set; }
    }
}