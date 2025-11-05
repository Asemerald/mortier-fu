using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MortierFu
{
    public class BombshellSystem : IGameSystem
    {
        public bool EnableDebug;

        // Load bombshell prefab through this opHandle
        private AsyncOperationHandle<GameObject> _prefabHandle;
        
        private IObjectPool<Bombshell> _pool;
        private const bool k_collectionCheck = true;
        private const int k_defaultCapacity = 10;
        private const int k_maxSize = 10000;
        
        // Track active bombshells
        private HashSet<Bombshell> _active;
        
        private bool _allowSelfDamage;
        
        private Transform _bombshellParent;
        private Collider[] _impactResults;

        private const int k_maxImpactTargets = 30;
        
        public BombshellSystem(bool enableDebug)
        {
            _allowSelfDamage = true;
            EnableDebug = enableDebug;
        }
        
        public Bombshell RequestBombshell(Bombshell.Data bombshellData)
        {
            Bombshell bombshell = _pool.Get();
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
                if(hit.TryGetComponent(out PlayerCharacter character)) {
                    // Prevent self-damage
                    if(!_allowSelfDamage && character == bombshell.Owner) 
                        continue; 
                    
                    if(!character.Health.IsAlive)
                        continue;
                    
                    character.Health.TakeDamage(bombshell.Damage, bombshell.Owner);
                    
                    if (EnableDebug)
                    {
                        Logs.Log("Bombshell hit " + character.name + " for " + bombshell.Damage + " damage.");
                    }
                }
                // temp check for breakable object
                else if (hit.TryGetComponent(out Breakable breakableObject))
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
                TEMP_CameraShake.Instance.CallCameraShake(bombshell.AoeRange, bombshell.Damage * 30, bombshell.Owner.CharacterStats.ProjectileTimeTravel.Value);
            }
            else Logs.LogWarning("No CameraShake");
            
            ReleaseBombshell(bombshell);
        }

        public void ClearActiveBombshells()
        {
            if (_active.Count <= 0) return;

            foreach (var bombshell in _active)
            {
                ReleaseBombshell(bombshell);
            }
        }
        
        #region Bombshell Callbacks
        
        private Bombshell OnCreateBombshell()
        {
            var go = Object.Instantiate(_prefabHandle.Result, _bombshellParent);
            var bombshell = go.GetComponent<Bombshell>();
            
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
            Object.Destroy(bombshell.gameObject);
        }
        
        #endregion
        
        public async Task OnInitialize()
        {
            // Load the bombshell prefab
            _prefabHandle = SystemManager.Config.BombshellPrefab.LoadAssetAsync<GameObject>(); 
            await _prefabHandle.Task;

            if (_prefabHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Logs.LogError("[BombshellManager]: Failed while loading Bombshell prefab through Addressables. Error: " + _prefabHandle.OperationException.Message);
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
            
            Addressables.Release(_prefabHandle);
        }
        
        public bool IsInitialized { get; set; }
    }
    
    // public class BombshellManager : MonoBehaviour
    // {
    //     public static BombshellManager Instance { get; private set; }
    //     
    //     [Header("References")]
    //     [SerializeField] private Bombshell _bombshellPrefab;
    //
    //     [Header("Debugging")]
    //     [SerializeField] private bool _enableDebug = true;
    //
    //     private GameModeBase _gmb; // TODO: TEMPORARY
    //     
    //     private List<Bombshell> _activeBombshells;
    //     private Collider[] _impactResults;
    //     
    //     private const int k_maxImpactTargets = 30;
    //
    //     private void Awake()
    //     {
    //         if (Instance != null)
    //         {
    //             Destroy(gameObject);
    //             return;
    //         }
    //
    //         Instance = this;
    //         
    //         _activeBombshells = new List<Bombshell>();
    //         _impactResults = new Collider[k_maxImpactTargets];
    //     }
    //
    //     public Bombshell RequestBombshell(Bombshell.Data bombshellData)
    //     {
    //         Bombshell bombshell = Instantiate(_bombshellPrefab, bombshellData.StartPos, Quaternion.identity, transform);
    //         bombshell.Initialize(this, bombshellData);
    //         _activeBombshells.Add(bombshell);
    //
    //         StartCoroutine(Test(bombshellData.TravelTime - 0.6f, bombshellData.TargetPos, bombshellData.AoeRange));
    //         
    //         return bombshell;
    //     }
    //
    //     private IEnumerator Test(float t, Vector3 pos, float aoeRange)
    //     {
    //         yield return new WaitForSeconds(t);
    //         if (TEMP_FXHandler.Instance)
    //         {
    //             TEMP_FXHandler.Instance.InstantiatePreview(pos, 0.6f, aoeRange);
    //         }
    //         else Logs.LogWarning("No FX Handler");
    //     }
    //     
    //     public void NotifyImpactAndRecycle(Bombshell bombshell)
    //     {
    //         int numHits = Physics.OverlapSphereNonAlloc(bombshell.transform.position, bombshell.AoeRange, _impactResults);
    //         
    //         for (int i = 0; i < numHits; i++)
    //         {
    //             Collider hit = _impactResults[i];
    //             if(hit.TryGetComponent(out PlayerCharacter character)) {
    //                 // Prevent self-damage
    //                 // if(character == bombshell.Owner) 
    //                 //     continue; 
    //                 
    //                 if(!character.Health.IsAlive)
    //                     continue;
    //                 
    //                 character.Health.TakeDamage(bombshell.Damage);
    //                 
    //                 if (_enableDebug)
    //                 {
    //                     Logs.Log("Bombshell hit " + character.name + " for " + bombshell.Damage + " damage.");
    //                 }
    //
    //                 if (!character.Health.IsAlive)
    //                 {
    //                     // TODO: COMPLETE CRAP, PLEASE DO BETTER OR I AM HAVING A HEART ATTACK
    //                     _gmb ??= FindFirstObjectByType<GameModeHolder>()?.Get();
    //                     _gmb?.NotifyKillEvent(bombshell.Owner, character);
    //                 }
    //             }
    //             // temp check for breakable object
    //             else if (hit.TryGetComponent(out Breakable breakableObject))
    //             {
    //                 breakableObject.DestroyObject(0);
    //             }
    //         }
    //         
    //         RecycleBombshell(bombshell);
    //     }
    //     
    //     public void RecycleBombshell(Bombshell bombshell)
    //     {
    //         if (!_activeBombshells.Contains(bombshell))
    //         {
    //             Logs.LogWarning("Trying to recycle a Bombshell that is not managed.");
    //             return;
    //         }
    //         
    //         _activeBombshells.Remove(bombshell);
    //         Destroy(bombshell.gameObject);
    //     }
    }
}