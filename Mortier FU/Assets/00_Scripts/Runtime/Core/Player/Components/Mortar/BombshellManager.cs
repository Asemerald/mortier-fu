using System.Collections;
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

        private GameModeBase _gmb; // TODO: TEMPORARY
        
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

            StartCoroutine(Test(bombshellData.TravelTime - 0.6f, bombshellData.TargetPos, bombshellData.AoeRange));
            
            return bombshell;
        }

        private IEnumerator Test(float t, Vector3 pos, float aoeRange)
        {
            yield return new WaitForSeconds(t);
            if (TEMP_FXHandler.Instance)
            {
                TEMP_FXHandler.Instance.InstantiatePreview(pos, 0.6f, aoeRange);
            }
            else Logs.LogWarning("No FX Handler");
        }
        
        public void NotifyImpactAndRecycle(Bombshell bombshell)
        {
            //GAMEFEEL CALLS
            if (TEMP_FXHandler.Instance)
            {
                TEMP_FXHandler.Instance.InstantiateExplosion(bombshell.transform.position, bombshell.AoeRange);
            }
            else Logs.LogWarning("No FX Handler");

            if (TEMP_CameraShake.Instance)
            {
                TEMP_CameraShake.Instance.CallCameraShake(bombshell.AoeRange, bombshell.Damage, bombshell.Owner.CharacterStats.ProjectileTimeTravel.Value);
            }
            else Logs.LogWarning("No CameraShake");
            
            
            int numHits = Physics.OverlapSphereNonAlloc(bombshell.transform.position, bombshell.AoeRange, _impactResults);
            
            for (int i = 0; i < numHits; i++)
            {
                Collider hit = _impactResults[i];
                if(hit.TryGetComponent(out PlayerCharacter character)) {
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
                        // TODO: COMPLETE CRAP, PLEASE DO BETTER OR I AM HAVING A HEART ATTACK
                        _gmb ??= FindFirstObjectByType<GameModeHolder>()?.Get();
                        _gmb?.NotifyKillEvent(bombshell.Owner, character);
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