using System.Collections.Generic;
using UnityEngine;

namespace MortierFu
{
    public abstract class LobbyInteractionZone : MonoBehaviour
    {
        private readonly HashSet<PlayerManager> _playersInside = new();
        private readonly HashSet<PlayerManager> _ignoredUntilExit = new();

        private void OnTriggerEnter(Collider other)
        {
            if (!TryGetPlayer(other, out PlayerManager player))
                return;

            if (!_playersInside.Add(player))
                return;

            if (_ignoredUntilExit.Contains(player))
                return;

            if (!CanEnter(player))
                return;

            OnPlayerEntered(player);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!TryGetPlayer(other, out PlayerManager player))
                return;

            if (!_playersInside.Remove(player))
                return;

            _ignoredUntilExit.Remove(player);

            OnPlayerExited(player);
        }

        protected virtual void OnDisable()
        {
            _playersInside.Clear();
            _ignoredUntilExit.Clear();
        }

        protected void IgnorePlayerUntilExit(PlayerManager player)
        {
            if (player)
                _ignoredUntilExit.Add(player);
        }

        protected virtual bool CanEnter(PlayerManager player) => player && player.ControlContext == PlayerControlContext.LobbySandbox;

        protected virtual void OnPlayerEntered(PlayerManager player)
        { }

        protected virtual void OnPlayerExited(PlayerManager player)
        { }

        private static bool TryGetPlayer(Collider other, out PlayerManager player)
        {
            player = null;

            if (!other)
                return false;

            PlayerCharacter character = other.GetComponentInParent<PlayerCharacter>();

            if (!character || !character.Owner)
                return false;

            player = character.Owner;
            return true;
        }
    }
}