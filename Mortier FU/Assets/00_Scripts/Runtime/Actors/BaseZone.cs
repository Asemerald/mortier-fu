using System;
using System.Collections.Generic;
using UnityEngine;

namespace MortierFu
{
    public abstract class BaseZone : MonoBehaviour
    {
        [SerializeField] protected float vfxFootPrintDuration;
        
        protected readonly Dictionary<PlayerCharacter, float> _counters = new();

        protected readonly List<PlayerCharacter> _playersCache = new();
        
        protected virtual void ApplyEffectZoneEnter(PlayerCharacter player){}

        protected virtual void ApplyEffectZoneExit(PlayerCharacter player){}

        protected virtual void ApplyEffectZoneTick(){}
        
    }
}