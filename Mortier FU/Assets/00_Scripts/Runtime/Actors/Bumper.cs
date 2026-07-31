using System;
using Cysharp.Threading.Tasks;
using FMODUnity;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MortierFu {
    public class Bumper : MonoBehaviour {
        [SerializeField] private float _bumpForce = 13.5f;
        [SerializeField] private float _bumpDuration = 0.5f;
        [SerializeField] private float _stunDuration = 0.75f;
        
        [SerializeField] private bool canPlaySpecialSound;

        private void OnCollisionEnter(Collision other) 
        {
            var rb = other.rigidbody;
            if (!rb) return;

            var character = rb.GetComponent<PlayerCharacter>();
            if (!character) return;

            var dir = -other.contacts[0].normal;
            character.ReceiveKnockback(_bumpDuration, dir * _bumpForce, _stunDuration, this);

            if (canPlaySpecialSound)
            {
                PlayFirstBumpSound().Forget();
                canPlaySpecialSound = false;
            }
        }

        private async UniTask PlayFirstBumpSound()
        {
            if (GetComponent<VehicleModel>().HalfLength < 2)
            {
                AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Misc_TuktukHonk, transform.position);
            }
            else
            {
                AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Misc_CarHonk, transform.position);
            }
            
            await UniTask.Delay(TimeSpan.FromSeconds(2), DelayType.DeltaTime);
            canPlaySpecialSound = true;
        }
    }
}