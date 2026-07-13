using System;
using System.Collections.Generic;
using UnityEngine;

namespace MortierFu
{
    public abstract class BaseZone : MonoBehaviour
    {
        [SerializeField] protected LayerMask _layerToIgnore;
        [SerializeField] protected float vfxFootPrintDuration;

        private readonly Dictionary<PlayerCharacter, float> _counters = new();

        private readonly List<PlayerCharacter> _playersCache = new();

        protected event Action<PlayerCharacter> OnTickZone; 

        #region Unity Lifecycle

        private void OnTriggerEnter(Collider other)
        {
            PlayerCharacter player = other.GetComponentInParent<PlayerCharacter>();

            if (!player || !_counters.TryAdd(player, vfxFootPrintDuration)) return;

            if (!IsPlayerValid(player))
            {
                _playersCache.Remove(player);
                _counters.Remove(player);
                return;
            }
            
            ApplyEffectZoneEnter(player);
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerCharacter player = other.GetComponentInParent<PlayerCharacter>();

            

            if (!player || !_counters.Remove(player)) return;

            if (!IsPlayerValid(player))
            {
                _playersCache.Remove(player);
                _counters.Remove(player);
                return;
            }

            if (CheckOtherZone(other)) return;
            
            ApplyEffectZoneExit(player,other);
        }

        protected virtual bool CheckOtherZone(Collider other)
        {
            Vector3 center = other.bounds.center;
            Vector3 halfExtents = other.bounds.extents;
            
            Collider[] hitColliders = Physics.OverlapBox(center, halfExtents,other.transform.rotation);
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.isTrigger && hitCollider.gameObject != gameObject && ((1 << hitCollider.gameObject.layer) & _layerToIgnore) != 0)
                {
                    return true;
                }
            }

            return false;
            
            
        }
        

        private void Update() => ApplyEffectZoneTick();

        private void OnDisable() => ClearPlayersCache();
        private void OnDestroy() => ClearPlayersCache();

        private void Reset()
        {
            Collider col = GetComponent<Collider>();

            if (col) col.isTrigger = true;
        }

        #endregion

        #region Template Method

        protected virtual void ApplyEffectZoneTick()
        {
            if (_counters.Count == 0) return;

            _playersCache.Clear();
            _playersCache.AddRange(_counters.Keys);

            foreach (var player in _playersCache)
            {

                if (!IsPlayerValid(player))
                {
                    _playersCache.Remove(player);
                    _counters.Remove(player);
                    return;
                }
                
                if (_counters[player] <= 0f)
                {
                    OnTickZone?.Invoke(player);
                    _counters[player] = vfxFootPrintDuration;
                }
                else
                {
                    _counters[player] -= Time.deltaTime;
                }
            }
        }

        protected abstract void ApplyEffectZoneEnter(PlayerCharacter player);

        protected abstract void ApplyEffectZoneExit(PlayerCharacter player, Collider other);

        protected virtual void PlayFootprintVFX(PlayerCharacter player) {}

        protected virtual bool IsPlayerValid(PlayerCharacter player)
        {
            if (!player) return false;

            return player.Health is { IsAlive: true };
        }

        protected virtual void ClearPlayersCache()
        {
            _playersCache.Clear();
            _counters.Clear();
        }

        #endregion
    }
}