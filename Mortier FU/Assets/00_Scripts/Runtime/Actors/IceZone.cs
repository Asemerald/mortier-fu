using System;
using UnityEngine;

namespace MortierFu
{
    public class IceZone : BaseZone
    {
        [SerializeField] private SpeedSettingsZone speedSettingsSpeedMultiplier;
        [SerializeField] private SpeedSettingsZone accelSettingsSpeedMultiplier;
        [SerializeField] private SpeedSettingsZone decelSettingsSpeedMultiplier;

        [SerializeField] private GameObject vfx;

        protected override void ApplyEffectZoneEnter(PlayerCharacter player)
        {
            player.SetExternalSpeedMultiplier
                (speedSettingsSpeedMultiplier.speedFactor, speedSettingsSpeedMultiplier.transitionDuration);

            player.SetExternalAccelerationMultiplier
                (accelSettingsSpeedMultiplier.speedFactor, accelSettingsSpeedMultiplier.transitionDuration);

            player.SetExternalDecelerationMultiplier
                (decelSettingsSpeedMultiplier.speedFactor, decelSettingsSpeedMultiplier.transitionDuration);
        }

        protected override void ApplyEffectZoneExit(PlayerCharacter player,Collider other)
        {
            player.SetExternalSpeedMultiplier
                (1, speedSettingsSpeedMultiplier.transitionDuration);

            player.SetExternalAccelerationMultiplier
                (1, accelSettingsSpeedMultiplier.transitionDuration);

            player.SetExternalDecelerationMultiplier
                (1, decelSettingsSpeedMultiplier.transitionDuration);
        }

        private void OnEnable()
        {
            OnTickZone += PlayFootprintVFX;
        }

        private void OnDisable()
        {
            OnTickZone -= PlayFootprintVFX;
        }

        protected override void PlayFootprintVFX(PlayerCharacter player)
        {
            if (vfx == null) return;

            var vfxInstance = Instantiate(vfx, player.FeetPoint.position,
                player.FeetPoint.rotation);

            Destroy(vfxInstance, 10f);
        }
    }

    [Serializable]
    struct SpeedSettingsZone
    {
        public float speedFactor;
        public float transitionDuration;
    }
}