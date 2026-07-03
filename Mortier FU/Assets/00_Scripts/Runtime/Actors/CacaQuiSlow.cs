using System;
using UnityEngine;

namespace MortierFu
{
    public class CacaQuiSlow : BaseZone
    {
        [SerializeField] private float slowMultiplier = 0.5f;
        [SerializeField] private float transitionDuration = 0.5f;
        [SerializeField] private GameObject vfxCacaQuiSlowPrefab;

        protected override void ApplyEffectZoneEnter(PlayerCharacter player)
        {
            player.SetExternalSpeedMultiplier(slowMultiplier, transitionDuration);
        }

        protected override void ApplyEffectZoneExit(PlayerCharacter player)
        {
            player.SetExternalSpeedMultiplier(1f, transitionDuration);
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
            if (player.ExternalSpeedMultiplier > slowMultiplier) return;

            var vfxInstance = Instantiate(vfxCacaQuiSlowPrefab, player.FeetPoint.position,
                player.FeetPoint.rotation);

            Destroy(vfxInstance, 10f);
        }
    }
}