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
        private GameModeBase _gameMode;
        
        private CameraSystem _cameraSystem;
        private FXService _fxService;

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
            if (!bombshell)
                return;

            if (_pool == null)
                return;

            if (_active == null || !_active.Contains(bombshell))
                return;

            _pool.Release(bombshell);
        }

        // Can be improved with a IDamageable interface which seems to be similar to IInteractable as interaction happens on impact or contact.
        public void NotifyImpact(Bombshell bombshell, RaycastHit hit)
        {
            Vector3 impactPoint = hit.point;

            var hitCharacters = new HashSet<PlayerCharacter>();
            var hits = new HashSet<GameObject>();

            int numHits = Physics.OverlapSphereNonAlloc(
                impactPoint,
                bombshell.AoeRange,
                _impactResults,
                Settings.WhatIsCollidable
            );

            for (int i = 0; i < numHits; i++)
            {
                Collider hitCollider = _impactResults[i];
                hits.Add(hitCollider.gameObject);

                Rigidbody rb = hitCollider.attachedRigidbody;
                if (!rb)
                    continue;

                if (rb.TryGetComponent(out PlayerCharacter character))
                {
                    if (_gameMode is { MatchConfig: { DisableSelfDamage: true } } && character == bombshell.Owner)
                        continue;

                    if (!character.CanPlayerInteractWithBombShell()) continue;

                    if (character.ControlContext is PlayerControlContext.AugmentRace)
                    {
                        character.ReceiveStun(character.Stats.GetKnockbackStunDuration());
                        continue;
                    }

                    bool damageApplied = character.Health.TakeDamage(
                        bombshell.Damage,
                        bombshell.Owner
                    );

                    if (!damageApplied)
                        continue;

                    hitCharacters.Add(character);

                    if (Settings.EnableDebug)
                    {
                        Logs.Log($"Bombshell from Player {bombshell.Owner.Owner.PlayerIndex} hit Player " +
                                 character.Owner.PlayerIndex + " for " + bombshell.Damage + " damage.");
                    }
                }
                else if (rb.TryGetComponent(out IInteractable interactable) && interactable.IsBombshellInteractable)
                {
                    Vector3 contactPoint = Physics.ClosestPoint(impactPoint, hitCollider,
                        hitCollider.transform.position, hitCollider.transform.rotation);

                    interactable.Interact(contactPoint);
                }
            }

            if (_fxService != null)
            {
                PlayerCharacter character = bombshell.Owner;
                _fxService.PlayBombshellExplosion(impactPoint, bombshell.AoeRange, character.Owner.PlayerIndex);
            }
            else
            {
                Logs.LogWarning("No FX Handler");
            }

            _cameraSystem.Controller.Shake(
                bombshell.AoeRange,
                20 + bombshell.Damage * 10,
                bombshell.GetTravelTime()
            );

            if (hitCharacters.Count > 0)
            {
                AudioService.PlayBombshellAudio(AudioService.FMODEvents.SFX_Mortar_ImpactPlayer, bombshell,
                    impactPoint);
            }
            else if (hits.Count > 0)
            {
                AudioService.PlayBombshellAudio(AudioService.FMODEvents.SFX_Mortar_ImpactProps, bombshell, impactPoint);
            }
            else
            {
                AudioService.PlayBombshellAudio(AudioService.FMODEvents.SFX_Mortar_ImpactNone, bombshell, impactPoint);
            }
            
            if (bombshell.Bounces > 0)
            {
                Logs.LogError($" bounces : {bombshell.Bounces}");
                AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Augment_Bounce, impactPoint);
            }

            if (hitCharacters.Count > 0)
            {
                EventBus<TriggerHit>.Raise(new TriggerHit()
                {
                    ShooterId = bombshell.Owner,
                    Bombshell = bombshell,
                    HitCharacters = hitCharacters.ToArray(),
                });
            }

            bool isGround = hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground");

            if (hits.Count > 0)
            {
                EventBus<TriggerBombshellImpact>.Raise(new TriggerBombshellImpact()
                {
                    Bombshell = bombshell,
                    HitPoint = impactPoint,
                    HitNormal = hit.normal,
                    HitGround = isGround,
                    HitObject = hit.collider.gameObject
                });
            }
        }

        public void ClearActiveBombshells()
        {
            if (_active == null || _active.Count <= 0)
                return;

            var activeBombshells = new List<Bombshell>(_active);

            for (int i = 0; i < activeBombshells.Count; i++)
            {
                ReleaseBombshell(activeBombshells[i]);
            }
        }

        #region Bombshell Callbacks

        private Bombshell OnCreateBombshell()
        {
            GameObject bombshellGo = Object.Instantiate(_bombshellPrefabHandle.Result, _bombshellParent);
            Bombshell bombshell = bombshellGo.GetComponent<Bombshell>();

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
            if (!bombshell)
                return;

            _active.Remove(bombshell);

            bombshell.OnRelease();
            bombshell.gameObject.SetActive(false);
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
            _settingsHandle = await SystemManager.Config.BombshellSettings.LazyLoadAssetRef();

            _bombshellPrefabHandle = await Settings.BombshellPrefab.LazyLoadAssetRef();

            _gameMode = GameService.CurrentGameMode as GameModeBase;
            _cameraSystem = SystemManager.Instance.Get<CameraSystem>();
            _fxService = ServiceManager.Instance.Get<FXService>();

            if (_cameraSystem == null || _fxService == null) return;

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