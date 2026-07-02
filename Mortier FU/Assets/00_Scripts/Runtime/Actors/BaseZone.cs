using System.Collections.Generic;
using UnityEngine;

namespace MortierFu
{
    public abstract class BaseZone : MonoBehaviour
    {
        [SerializeField] protected float vfxFootPrintDuration;

        private readonly Dictionary<PlayerCharacter, float> _counters = new();

        private readonly List<PlayerCharacter> _playersCache = new();

        #region Unity Lifecycle

        private void OnTriggerEnter(Collider other)
        {
            PlayerCharacter player = other.GetComponentInParent<PlayerCharacter>();

            if (!player || !_counters.TryAdd(player, vfxFootPrintDuration)) return;

            ApplyEffectZoneEnter(player);
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerCharacter player = other.GetComponentInParent<PlayerCharacter>();

            if (!player || !_counters.Remove(player)) return;

            ApplyEffectZoneExit(player);
        }

        private void Update() => ApplyEffectZoneTick();

        private void OnDisable()
        {
            _counters.Clear();
            _playersCache.Clear();
        }

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
                if (_counters[player] <= 0f)
                {
                    PlayFootprintVFX(player);
                    _counters[player] = vfxFootPrintDuration;
                }
                else
                {
                    _counters[player] -= Time.deltaTime;
                }
            }
        }

        protected abstract void ApplyEffectZoneEnter(PlayerCharacter player);

        protected abstract void ApplyEffectZoneExit(PlayerCharacter player);

        protected virtual void PlayFootprintVFX(PlayerCharacter player) {}

        #endregion
    }
}