using System.Collections.Generic;
using UnityEngine;

namespace MortierFu
{
    public class CacaQuiSlow : MonoBehaviour
    {
        [SerializeField] private float slowMultiplier = 0.5f;
        [SerializeField] private float transitionDuration = 0.5f;

        // Track how many slow zones are affecting each player
        private readonly Dictionary<PlayerCharacter, int> _counters = new();

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerCharacter>();
            if (player == null) return;

            if (!_counters.TryGetValue(player, out var count))
                count = 0;

            count++;
            _counters[player] = count;

            if (count == 1)
            {
                player.SetExternalSpeedMultiplier(slowMultiplier, transitionDuration);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var player = other.GetComponentInParent<PlayerCharacter>();
            if (player == null) return;

            if (!_counters.TryGetValue(player, out var count)) return;

            count--;
            if (count <= 0)
            {
                _counters.Remove(player);
                player.SetExternalSpeedMultiplier(1f, transitionDuration);
            }
            else
            {
                _counters[player] = count;
            }
        }
    }
}