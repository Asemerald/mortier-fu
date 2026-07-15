using System;
using System.Collections.Generic;
using MortierFu.Shared;
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
                if (_counters.Remove(player))
                    ApplyEffectZoneExit(player, null);
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

            ApplyEffectZoneExit(player,other);
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
            foreach (var player in _playersCache)
            {
                if (player) ApplyEffectZoneExit(player, null);
            }
            _playersCache.Clear();
            _counters.Clear();
        }

        #endregion
    }
}