using System;
using System.Collections.Generic;
using UnityEngine;

namespace MortierFu
{
    /// <summary>
    /// For the moment need element to trigger it in the script that herit from it
    /// For exemple CacaQuiSlow use OnTrigger...
    /// </summary>
    public abstract class BaseZone : MonoBehaviour
    {
        [SerializeField] protected float vfxFootPrintDuration;
        
        protected readonly Dictionary<PlayerCharacter, float> _counters = new();

        protected readonly List<PlayerCharacter> _playersCache = new();
        
        protected virtual void ApplyEffectZoneEnter(PlayerCharacter player){}

        protected virtual void ApplyEffectZoneExit(PlayerCharacter player){}

        protected virtual void ApplyEffectZoneTick(){}
        
        private void Reset()
        {
            Collider col = GetComponent<Collider>();
            
            if (col) col.isTrigger = true;
            
            _counters.Clear();
            _playersCache.Clear();
        }
        
    }
}