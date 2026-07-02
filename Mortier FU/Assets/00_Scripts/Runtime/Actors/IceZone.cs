using System;
using UnityEngine;

namespace MortierFu
{
    public class IceZone : BaseZone
    {
        #region Variables
        
        [SerializeField] SpeedSettingsZone speedSettingsSpeedMultiplier;
        [SerializeField] SpeedSettingsZone accelSettingsSpeedMultiplier;
        [SerializeField] SpeedSettingsZone decelSettingsSpeedMultiplier;
        
        [SerializeField] private GameObject vfx;
        
        #endregion
        
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

        protected override void ApplyEffectZoneEnter(PlayerCharacter player)
        {
            player.SetExternalSpeedMultiplier
                (speedSettingsSpeedMultiplier.speedFactor, speedSettingsSpeedMultiplier.transitionDuration);
            
            player.SetExternalAccelerationMultiplier
                (accelSettingsSpeedMultiplier.speedFactor, accelSettingsSpeedMultiplier.transitionDuration);
            
            player.SetExternalDecelerationMultiplier
                (decelSettingsSpeedMultiplier.speedFactor, decelSettingsSpeedMultiplier.transitionDuration);
        }

        protected override void ApplyEffectZoneExit(PlayerCharacter player)
        {
            player.SetExternalSpeedMultiplier
                (1, speedSettingsSpeedMultiplier.transitionDuration);
            
            player.SetExternalAccelerationMultiplier
                (1, accelSettingsSpeedMultiplier.transitionDuration);
            
            player.SetExternalDecelerationMultiplier
                (1, decelSettingsSpeedMultiplier.transitionDuration);
        }

        protected override void ApplyEffectZoneTick()
        {
            int counter = _counters.Count;
            
            if (counter == 0) return;
            
            _playersCache.Clear();
            _playersCache.AddRange(_counters.Keys);
            
            foreach (var player in  _playersCache)
            {
                if (_counters[player] <= 0f)
                {
                    PlayFxIceFootPrint(player);
                    _counters[player] = vfxFootPrintDuration;
                }
                else
                {
                    _counters[player] -= Time.deltaTime;
                }
            }
        }

        private void PlayFxIceFootPrint(PlayerCharacter player)
        {
            if (vfx == null) return;

            var caca = Instantiate(vfx, player.FeetPoint.position,
                player.FeetPoint.rotation);
            
            Destroy(caca, 10f);
        }
        
    }
    

    [Serializable]
    struct SpeedSettingsZone
    {
        public float speedFactor;
        public float transitionDuration;
    }
}