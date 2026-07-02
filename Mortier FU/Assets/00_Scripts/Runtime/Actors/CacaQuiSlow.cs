using System;
using System.Collections.Generic;
using UnityEngine;

namespace MortierFu
{
    public class CacaQuiSlow : MonoBehaviour
    {
        #region Variables

        
        [SerializeField] private float slowMultiplier = 0.5f;
        [SerializeField] private float transitionDuration = 0.5f;
        
        [SerializeField] private GameObject vfxCacaQuiSlowPrefab;
        
        [SerializeField] private float vfxCacaQuiSlowDuration = 0.5f;

        private readonly Dictionary<PlayerCharacter, float> _counters = new();

        private readonly List<PlayerCharacter> _playersCache = new();
        
        #endregion

        #region Unity Lifecycle

        private void OnTriggerEnter(Collider other)
        {
            PlayerCharacter player = other.GetComponentInParent<PlayerCharacter>();
            
            if (!player || !_counters.TryAdd(player, vfxCacaQuiSlowDuration)) return;

            player.SetExternalSpeedMultiplier(slowMultiplier, transitionDuration);
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerCharacter player = other.GetComponentInParent<PlayerCharacter>();
            
            if (!player || !_counters.Remove(player)) return;

            player.SetExternalSpeedMultiplier(1f, transitionDuration);
        }

        
        private void Update() => UpdateVfxCacaQuiSlowVFX();

        private void UpdateVfxCacaQuiSlowVFX()
        {
            int counter = _counters.Count;
            
            if (counter == 0) return;
            
            _playersCache.Clear();
            _playersCache.AddRange(_counters.Keys);
            
            foreach (var player in  _playersCache)
            {
                if (_counters[player] <= 0f)
                {
                    PlayCacaQuiSlowVFX(player);
                    _counters[player] = vfxCacaQuiSlowDuration;
                }
                else
                {
                    _counters[player] -= Time.deltaTime;
                }
            }

            
        }

        #endregion

        private void PlayCacaQuiSlowVFX(PlayerCharacter player)
        {
            if (player.ExternalSpeedMultiplier > 0.5f) return;
            
            var caca = Instantiate(vfxCacaQuiSlowPrefab, player.FeetPoint.position,
                player.FeetPoint.rotation);
            
            Destroy(caca, 10f);
        }
        
        private void Reset()
        {
            Collider col = GetComponent<Collider>();
            
            if (col) col.isTrigger = true;
            
            _counters.Clear();
            _playersCache.Clear();
        }
        
    }
}