using System.Collections.Generic;
using UnityEngine;

namespace MortierFu
{
    public class PuddleController : MonoBehaviour
    {
        public List<Ability> Abilities;
        public float Lifetime = 5f;

        private readonly HashSet<PlayerCharacter> _inside = new();

        private void Start()
        {
            Destroy(gameObject, Lifetime);
        }

        private void OnTriggerEnter(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (rb == null || !rb.TryGetComponent(out PlayerCharacter character)) return;

            _inside.Add(character);
            ApplyEffects(character);
        }

        private void OnTriggerExit(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (rb == null || !rb.TryGetComponent(out PlayerCharacter character)) return;

            CancelEffects(character);
            _inside.Remove(character);
        }

        private void OnDestroy()
        {
            foreach (var player in _inside)
                CancelEffects(player);
        }

        private void ApplyEffects(PlayerCharacter target)
        {
            foreach (Ability t in Abilities)
            {
                t.Execute(target);
            }
        }

        private void CancelEffects(PlayerCharacter target)
        {
            foreach (Ability t in Abilities)
            {
                t.Cancel(target);
            }
        }
    }
}