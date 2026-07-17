using System;
using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public abstract class BaseZone : MonoBehaviour
    {
        [SerializeField] protected float vfxFootPrintDuration;
        
        private readonly Dictionary<PlayerCharacter, float> _counters = new();

        private readonly List<PlayerCharacter> _playersCache = new();

        protected event Action<PlayerCharacter> OnTickZone;

        private float _timer = 0f;
        private bool _ShouldCheckThisFrame;

        private Collider _collider;

        #region Unity Lifecycle

        private void Start()
        {
            _collider = GetComponent<Collider>();
        }
        
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

            if (!player || !_counters.Remove(player))
            {
                ApplyEffectZoneExit(player, null);
                return;
            }

            if (!IsPlayerValid(player))
            {
                _playersCache.Remove(player);
                _counters.Remove(player);
                ApplyEffectZoneExit(player, null);
                return;
            }

            ApplyEffectZoneExit(player, other);
        }

        private void Update() => ApplyEffectZoneTick();

        private void OnDisable() => ClearPlayersCache();
        private void OnDestroy() => ClearPlayersCache();

        private void Reset()
        {

            if (_collider) _collider.isTrigger = true;
        }

        #endregion

        #region Template Method

        protected virtual void ApplyEffectZoneTick()
        {
            if (_counters.Count == 0) return;

            _playersCache.Clear();
            _playersCache.AddRange(_counters.Keys);

            _timer += Time.deltaTime;
            if (_timer >= 1f)
            {
                _ShouldCheckThisFrame = true;
                _timer = 0f;
            }

            foreach (var player in _playersCache)
            {
                
                if (!IsPlayerValid(player))
                {
                    _playersCache.Remove(player);
                    _counters.Remove(player);
                    ApplyEffectZoneExit(player, null);
                    return;
                }
                
                if (_ShouldCheckThisFrame && !_collider.bounds.Contains(player.transform.position))
                {
                    _counters.Remove(player);
                    ApplyEffectZoneExit(player, null);
                    continue;
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
            if(_ShouldCheckThisFrame)
                _ShouldCheckThisFrame =  false;
        }

        protected abstract void ApplyEffectZoneEnter(PlayerCharacter player);

        protected abstract void ApplyEffectZoneExit(PlayerCharacter player, Collider other);

        protected virtual void PlayFootprintVFX(PlayerCharacter player)
        {
        }

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