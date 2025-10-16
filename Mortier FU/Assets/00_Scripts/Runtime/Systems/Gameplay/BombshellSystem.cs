using MortierFu.Shared;
using UnityEngine;
using UnityEngine.Pool;

namespace MortierFu
{
    public class BombshellSystem : IGameSystem
    {
        // TODO: Remove this when we talk about service and system identification
        // Causes system duplicity issues
        private static BombshellSystem _instance;
        public static BombshellSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BombshellSystem();
                }
                
                return _instance;
            }
        }

        private const bool k_collectionCheck = true;
        private const int k_defaultCapacity = 10;
        private const int k_maxSize = 20;

        [Header("References")] // TODO: Load from Addressables
        [SerializeField] private Bombshell _bombshellPrefab; // No longer an Object, cannot serialize and provide through inspector

        [Header("Debugging")] 
        public bool EnableDebug = true;
        
        private IObjectPool<Bombshell> _pool;
        private Transform _bombshellParent;
        
        private Collider[] _impactResults;
        private const int k_maxImpactTargets = 30;

        public Bombshell RequestBombshell(Bombshell.Data bombshellData)
        {
            Bombshell bombshell = _pool.Get();
            bombshell.Configure(bombshellData);
            bombshell.transform.position = bombshellData.StartPos;
            bombshell.transform.rotation = Quaternion.identity;
            
            return bombshell;
        }
        
        public void NotifyImpactAndRecycle(Bombshell bombshell)
        {
            int numHits = Physics.OverlapSphereNonAlloc(bombshell.transform.position, bombshell.AoeRange, _impactResults);
            for (int i = 0; i < numHits; i++)
            {
                Collider hit = _impactResults[i];
                if(hit.TryGetComponent(out Character character)) {
                    // Prevent self-damage
                    if(character == bombshell.Owner) 
                        continue; 
                    
                    if(!character.Health.IsAlive)
                        continue;
                    
                    character.Health.TakeDamage(bombshell.Damage);
                    
                    if (EnableDebug)
                    {
                        Logs.Log("Bombshell hit " + character.name + " for " + bombshell.Damage + " damage.");
                    }

                    if (!character.Health.IsAlive)
                    {
                        GM_Base.Instance.NotifyKillEvent(bombshell.Owner, character);
                    }
                }
            }
            
            RecycleBombshell(bombshell);
        }
        
        public void RecycleBombshell(Bombshell bombshell)
        {
            _pool.Release(bombshell);
        }
        
        #region Object Pool Callbacks
        void OnBombshellGet(Bombshell bombshell)
        {
            bombshell.gameObject.SetActive(true);
        }

        void OnBombshellReleased(Bombshell bombshell)
        {
            bombshell.gameObject.SetActive(false);
        }
        
        Bombshell OnBombshellCreated()
        {
            var bombshell = Object.Instantiate(_bombshellPrefab, _bombshellParent);
            bombshell.gameObject.SetActive(false);
            
            return bombshell;
        }

        void OnBombshellDestroyed(Bombshell bombshell)
        {
            Object.Destroy(bombshell.gameObject);
        }
        #endregion

        public void Initialize()
        {
            _bombshellParent = new GameObject("Bombshells").transform;
            _pool = new ObjectPool<Bombshell>(
                OnBombshellCreated,
                OnBombshellGet,
                OnBombshellReleased,
                OnBombshellDestroyed,
                k_collectionCheck,
                k_defaultCapacity,
                k_maxSize
            );
            
            // TODO: Move damage / impact handling to another system
            _impactResults = new Collider[k_maxImpactTargets];
        }

        public void Tick()
        { }
        
        public void Dispose()
        {
            _pool.Clear();
            
            if (_bombshellParent != null)
            {
                Object.Destroy(_bombshellParent.gameObject);
            }
        }
    }
}