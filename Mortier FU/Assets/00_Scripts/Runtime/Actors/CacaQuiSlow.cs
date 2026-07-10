using System;
using UnityEngine;
using Random = UnityEngine.Random;

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

        protected override void ApplyEffectZoneExit(PlayerCharacter player,Collider other)
        {
            Debug.Log("out");
            Debug.Log("effectRemoved");
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

            float randomZRotation = Random.value * 360f;

            Vector3 baseEuler = vfxCacaQuiSlowPrefab.transform.rotation.eulerAngles;
            Quaternion finalRotationFx = Quaternion.Euler(baseEuler.x, baseEuler.y, baseEuler.z + randomZRotation);

            var vfxInstance = Instantiate(vfxCacaQuiSlowPrefab, player.FeetPoint.position, finalRotationFx);

            Destroy(vfxInstance, 10f);
        }

        
    }
}