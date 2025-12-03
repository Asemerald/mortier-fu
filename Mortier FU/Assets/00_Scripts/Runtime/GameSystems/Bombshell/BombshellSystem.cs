using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.Pool;

namespace MortierFu
{
    public class BombshellSystem : IGameSystem
    {
        public SO_BombshellSettings Settings { get; private set; }
        private GameObject _bombshellPrefab;

        private IObjectPool<Bombshell> _pool;
        private const bool k_collectionCheck = true;
        private const int k_defaultCapacity = 30;
        private const int k_maxSize = 10000;

        // Track active bombshells
        private HashSet<Bombshell> _active;

        private Transform _bombshellParent;
        private Collider[] _impactResults;
        private CameraSystem _cameraSystem;

        private const int k_maxImpactTargets = 50;

        public Bombshell RequestBombshell(Bombshell.Data bombshellData)
        {
            Bombshell bombshell = _pool.Get();
            bombshellData.Height = Settings.BombshellHeight;
            bombshell.SetData(bombshellData);
            bombshell.OnGet();
            bombshell.gameObject.SetActive(true);
            return bombshell;
        }

        public void ReleaseBombshell(Bombshell bombshell)
        {
            _pool.Release(bombshell);
        }

        // Can be improved with a IDamageable interface which seems to be similar to IInteractable as interaction happens on impact or contact.
        public void NotifyImpact(Bombshell bombshell)
        {
            var hitCharacters = new HashSet<PlayerCharacter>();
            var hits = new HashSet<GameObject>();
            
            int numHits = Physics.OverlapSphereNonAlloc(bombshell.transform.position, bombshell.AoeRange, _impactResults);
            for (int i = 0; i < numHits; i++)
            {
                Collider hit = _impactResults[i];

                if (hit.attachedRigidbody == null)
                {
                    hits.Add(hit.gameObject);
                    continue;
                }

                if (hit.attachedRigidbody.TryGetComponent(out PlayerCharacter character))
                {
                    // Prevent self-damage
                    if (!Settings.AllowSelfDamage && character == bombshell.Owner)
                        continue;

                    if (!character.Health.IsAlive)
                        continue;

                    hitCharacters.Add(character);

                    character.Health.TakeDamage(bombshell.Damage, bombshell.Owner);

                    if (Settings.EnableDebug)
                    {
                        Logs.Log("Bombshell hit " + character.name + " for " + bombshell.Damage + " damage.");
                    }
                }
                // temp check for breakable object
                else if (hit.attachedRigidbody.TryGetComponent(out IInteractable interactable) &&
                         interactable.IsBombshellInteractable)
                {
                    interactable.Interact();
                }
            }

            //GAMEFEEL CALLS
            if (TEMP_FXHandler.Instance)
            {
                TEMP_FXHandler.Instance.InstantiateExplosion(bombshell.transform.position, bombshell.AoeRange);
            }
            else Logs.LogWarning("No FX Handler");

            _cameraSystem.Controller.Shake(bombshell.AoeRange, 20 + bombshell.Damage * 10,
                bombshell.Owner.Stats.BombshellTimeTravel.Value);

            if (hitCharacters.Count > 0)
            {
                EventBus<TriggerHit>.Raise(new TriggerHit()
                {
                    Bombshell = bombshell,
                    HitCharacters = hitCharacters.ToArray(),
                });
            }

            if (hits.Count > 0)
            {
                EventBus<TriggerBombshellImpact>.Raise(new TriggerBombshellImpact()
                {
                    Bombshell = bombshell,
                    Hits = hits.ToArray(),
                });
            }
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
            var go = Object.Instantiate(_bombshellPrefab, _bombshellParent);
            var bombshell = go.GetComponent<Bombshell>();

            bombshell.Initialize(this);
            go.SetActive(false);

            return bombshell;
        }

        private void OnGetBombshell(Bombshell bombshell)
        {
            _active.Add(bombshell);
        }

        private void OnReleaseBombshell(Bombshell bombshell)
        {
            bombshell.OnRelease();
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
            var settingsRef = SystemManager.Config.BombshellSettings;
            Settings = await AddressablesUtils.LazyLoadAsset(settingsRef);
            if (Settings == null) return;

            _cameraSystem = SystemManager.Instance.Get<CameraSystem>();
            if (_cameraSystem == null) return;
            
            _bombshellPrefab = await AddressablesUtils.LazyLoadAsset(Settings.BombshellPrefab);
            if (_bombshellPrefab == null) return;

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
        }

        public bool IsInitialized { get; set; }
    }
}