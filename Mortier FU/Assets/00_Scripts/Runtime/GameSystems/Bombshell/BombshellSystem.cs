using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MortierFu
{
    public class BombshellSystem : IGameSystem
    {
        // Load bombshell prefab through this opHandle
        private AsyncOperationHandle<SO_BombshellSettings> _settingsHandle; 
        private AsyncOperationHandle<GameObject> _prefabHandle;
        
        private IObjectPool<Bombshell> _pool;
        private const bool k_collectionCheck = true;
        private const int k_defaultCapacity = 30;
        private const int k_maxSize = 10000;
        
        // Track active bombshells
        private HashSet<Bombshell> _active;
        
        private Transform _bombshellParent;
        private Collider[] _impactResults;

        private const int k_maxImpactTargets = 50;
        
        public SO_BombshellSettings Settings => _settingsHandle.Result;

        public Bombshell RequestBombshell(Bombshell.Data bombshellData)
        {
            Bombshell bombshell = _pool.Get();
            bombshellData.Height = Settings.BombshellHeight;
            bombshell.SetData(bombshellData);
            return bombshell;
        }

        public void ReleaseBombshell(Bombshell bombshell)
        {
            _pool.Release(bombshell);
        }

        public void NotifyImpact(Bombshell bombshell)
        {
            int numHits = Physics.OverlapSphereNonAlloc(bombshell.transform.position, bombshell.AoeRange, _impactResults);
            
            for (int i = 0; i < numHits; i++)
            {
                Collider hit = _impactResults[i];
                if(hit.attachedRigidbody == null) continue;
                
                if(hit.attachedRigidbody.TryGetComponent(out PlayerCharacter character)) {
                    // Prevent self-damage
                    if(!Settings.AllowSelfDamage && character == bombshell.Owner) 
                        continue; 
                    
                    if(!character.Health.IsAlive)
                        continue;
                    
                    character.Health.TakeDamage(bombshell.Damage, bombshell.Owner);
                    
                    if (Settings.EnableDebug)
                    {
                        Logs.Log("Bombshell hit " + character.name + " for " + bombshell.Damage + " damage.");
                    }
                }
                // temp check for breakable object
                else if (hit.attachedRigidbody.TryGetComponent(out Breakable breakableObject))
                {
                    breakableObject.DestroyObject(0);
                }
            }
            
            //GAMEFEEL CALLS
            if (TEMP_FXHandler.Instance)
            {
                TEMP_FXHandler.Instance.InstantiateExplosion(bombshell.transform.position, bombshell.AoeRange);
            }
            else Logs.LogWarning("No FX Handler");

            if (TEMP_CameraShake.Instance)
            {
                TEMP_CameraShake.Instance.CallCameraShake(bombshell.AoeRange, 20 + bombshell.Damage * 10, bombshell.Owner.CharacterStats.BombshellTimeTravel.Value);
            }
            else Logs.LogWarning("No CameraShake");
            
            ReleaseBombshell(bombshell);
        }

        public void ClearActiveBombshells()
        {
            if (_active.Count <= 0) return;
            
            // Release all active bombshells, this remove them from hash set so must not foreach
            var activeBombshells = new List<Bombshell>(_active);
            for (int i = 0; i < activeBombshells.Count; i++)
            {
                ReleaseBombshell(activeBombshells[i]);
            }
        }
        
        #region Bombshell Callbacks
        
        private Bombshell OnCreateBombshell()
        {
            var go = Object.Instantiate(_prefabHandle.Result, _bombshellParent);
            var bombshell = go.GetComponent<Bombshell>();
            
            bombshell.Initialize(this);
            go.SetActive(false);
            
            return bombshell;
        }

        private void OnGetBombshell(Bombshell bombshell)
        {
            bombshell.gameObject.SetActive(true);
            
            _active.Add(bombshell);
        }

        private void OnReleaseBombshell(Bombshell bombshell)
        {
            bombshell.gameObject.SetActive(false);
            
            _active.Remove(bombshell);
        }

        private void OnDestroyBombshell(Bombshell bombshell)
        {
            if (bombshell && bombshell.gameObject)
            {
                Object.Destroy(bombshell.gameObject);
            }
        }
        
        #endregion
        
        public async UniTask OnInitialize()
        {
            // Load the system settings
            _settingsHandle = SystemManager.Config.BombshellSettings.LoadAssetAsync();
            await _settingsHandle;

            if (_settingsHandle.Status != AsyncOperationStatus.Succeeded)
            {
                if (Settings.EnableDebug)
                {
                    Logs.LogError("[BombshellManager]: Failed while loading settings with Addressables. Error: " + _prefabHandle.OperationException.Message);
                }
                return;
            }
            
            // Load the bombshell prefab
            _prefabHandle = Settings.BombshellPrefab.LoadAssetAsync(); 
            await _prefabHandle;
            
            if (_prefabHandle.Status != AsyncOperationStatus.Succeeded)
            {
                if (Settings.EnableDebug)
                {
                    Logs.LogError("[BombshellManager]: Failed while loading Bombshell prefab through Addressables. Error: " + _prefabHandle.OperationException.Message);
                }
                return;
            }
            
            _bombshellParent = new GameObject("Bombshells").transform;

            _active = new HashSet<Bombshell>();
            _pool = new ObjectPool<Bombshell>(
                OnCreateBombshell,
                OnGetBombshell,
                OnReleaseBombshell,
                OnDestroyBombshell,
                k_collectionCheck,
                k_defaultCapacity,
                k_maxSize
            );

            _impactResults = new Collider[k_maxImpactTargets];
        }
        
        public void Dispose()
        {
            _pool.Clear();
            _active.Clear();
            
            Addressables.Release(_settingsHandle);
            Addressables.Release(_prefabHandle);
        }
        
        public bool IsInitialized { get; set; }
    }
}