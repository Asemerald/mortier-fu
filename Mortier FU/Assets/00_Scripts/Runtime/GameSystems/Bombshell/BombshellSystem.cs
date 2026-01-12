using System.Collections.Generic;
using System.Linq;
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
        private CameraSystem _cameraSystem;

        private IObjectPool<Bombshell> _pool;
        private const bool k_collectionCheck = true;
        private const int k_defaultCapacity = 30;
        private const int k_maxSize = 10000;

        // Track active bombshells
        private HashSet<Bombshell> _active;
        private Transform _bombshellParent;
        
        private Collider[] _impactResults;
        private const int k_maxImpactTargets = 50;
        
        private AsyncOperationHandle<SO_BombshellSettings> _settingsHandle;
        public SO_BombshellSettings Settings => _settingsHandle.Result;
        
        private AsyncOperationHandle<GameObject> _bombshellPrefabHandle;
        
        public Bombshell RequestBombshell(Bombshell.Data bombshellData)
        {
            Bombshell bombshell = _pool.Get();
            bombshell.Configure(bombshellData);
            bombshell.OnGet();
            bombshell.gameObject.SetActive(true);
            return bombshell;
        }

        public void ReleaseBombshell(Bombshell bombshell)
        {
            _pool.Release(bombshell);
        }

        // Can be improved with a IDamageable interface which seems to be similar to IInteractable as interaction happens on impact or contact.
        public void NotifyImpact(Bombshell bombshell, RaycastHit hit)
        {
            var hitCharacters = new HashSet<PlayerCharacter>();
            var hits = new HashSet<GameObject>();
            
            int numHits = Physics.OverlapSphereNonAlloc(bombshell.transform.position, bombshell.AoeRange, _impactResults, Settings.WhatIsCollidable);
            for (int i = 0; i < numHits; i++)
            {
                Collider hitCollider = _impactResults[i];
                hits.Add(hitCollider.gameObject);

                var rb = hitCollider.attachedRigidbody;
                if (rb && rb.TryGetComponent(out PlayerCharacter character))
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
                        Logs.Log($"Bombshell from Player {bombshell.Owner.Owner.PlayerIndex} hit Player " + character.Owner.PlayerIndex + " for " + bombshell.Damage + " damage.");
                    }
                }
                // temp check for breakable object
                else if (hitCollider.TryGetComponent(out IInteractable interactable) &&
                         interactable.IsBombshellInteractable)
                {
                    interactable.Interact();
                }
            }

            //GAMEFEEL CALLS
            if (TEMP_FXHandler.Instance)
            {
                var character = bombshell.Owner;
                TEMP_FXHandler.Instance.InstantiateExplosion(hit.point, bombshell.AoeRange, character.Owner.PlayerIndex);
            }
            else Logs.LogWarning("No FX Handler");

            _cameraSystem.Controller.Shake(bombshell.AoeRange, 20 + bombshell.Damage * 10,
                bombshell.GetTravelTime());
            
            if (hitCharacters.Count > 0)
            {
                EventBus<TriggerHit>.Raise(new TriggerHit()
                {
                    ShooterId = bombshell.Owner,
                    Bombshell = bombshell,
                    HitCharacters = hitCharacters.ToArray(),
                });
            }

            //TODO: Maybe a better way to only spawn puddle on ground hit?
            bool isGround = hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground");
            
            if (hits.Count > 0)
            {
                EventBus<TriggerBombshellImpact>.Raise(new TriggerBombshellImpact()
                {
                    Bombshell = bombshell,
                    HitPoint = hit.point,
                    HitNormal = hit.normal,
                    HitGround = isGround,
                    HitObject = hit.collider.gameObject
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
            var bombshellGo = Object.Instantiate(_bombshellPrefabHandle.Result, _bombshellParent);
            var bombshell = bombshellGo.GetComponent<Bombshell>();

            bombshell.Initialize(this);
            bombshellGo.SetActive(false);

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
            // Load the system settings;lla
            _settingsHandle = await SystemManager.Config.BombshellSettings.LazyLoadAssetRef();

            _bombshellPrefabHandle = await Settings.BombshellPrefab.LazyLoadAssetRef();
            
            _cameraSystem = SystemManager.Instance.Get<CameraSystem>();
            if (_cameraSystem == null) return;
            
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
            Addressables.Release(_bombshellPrefabHandle);
            Addressables.Release(_settingsHandle);
            
            _pool.Clear();
            _active.Clear();
        }

        public bool IsInitialized { get; set; }
    }
}