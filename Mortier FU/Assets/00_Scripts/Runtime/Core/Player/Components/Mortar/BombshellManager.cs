using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class BombshellManager : MonoBehaviour
    {
        public static BombshellManager Instance { get; private set; }
        
        [Header("References")]
        [SerializeField] private Bombshell _bombshellPrefab;

        [Header("Debugging")]
        [SerializeField] private bool _enableDebug = true;
        
        private List<Bombshell> _activeBombshells;
        private Collider[] _impactResults;
        
        private const int k_maxImpactTargets = 30;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            
            _activeBombshells = new List<Bombshell>();
            _impactResults = new Collider[k_maxImpactTargets];
        }

        public Bombshell RequestBombshell(Bombshell.Data bombshellData)
        {
            Bombshell bombshell = Instantiate(_bombshellPrefab, bombshellData.StartPos, Quaternion.identity, transform);
            bombshell.Initialize(this, bombshellData);
            _activeBombshells.Add(bombshell);
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
                    
                    if (_enableDebug)
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
            if (!_activeBombshells.Contains(bombshell))
            {
                Logs.LogWarning("Trying to recycle a Bombshell that is not managed.");
                return;
            }
            
            _activeBombshells.Remove(bombshell);
            Destroy(bombshell.gameObject);
        }
    }
}