using UnityEngine;

namespace MortierFu {
    public class Bumper : MonoBehaviour {
        [SerializeField] private float _bumpForce = 13.5f;
        [SerializeField] private float _bumpDuration = 0.5f;
        [SerializeField] private float _stunDuration = 0.75f;
    
        private void OnCollisionEnter(Collision other) {
            var rb = other.rigidbody;
            if (!rb) return;

            var character = rb.GetComponent<PlayerCharacter>();
            if (!character) return;

            var dir = -other.contacts[0].normal;
            character.ReceiveKnockback(_bumpDuration, dir * _bumpForce, _stunDuration);
        }
    }
}