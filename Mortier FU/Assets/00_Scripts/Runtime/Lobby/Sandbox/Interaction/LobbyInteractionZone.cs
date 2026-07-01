using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public abstract class LobbyInteractionZone : MonoBehaviour, IPlayerInteractionHandler
    {
        protected readonly HashSet<PlayerManager> PlayersInside = new();

        private PlayerInteractionService InteractionService =>
            ServiceManager.Instance?.Get<PlayerInteractionService>();

        private void OnTriggerEnter(Collider other)
        {
            if (!TryGetPlayer(other, out var player))
                return;

            if (!PlayersInside.Add(player))
                return;

            InteractionService?.Register(player, this);

            OnPlayerEntered(player);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!TryGetPlayer(other, out var player))
                return;

            if (!PlayersInside.Remove(player))
                return;

            InteractionService?.Unregister(player, this);

            OnPlayerExited(player);
        }

        protected virtual void OnDisable()
        {
            UnregisterAllPlayers();
            PlayersInside.Clear();
        }

        protected virtual void OnPlayerEntered(PlayerManager player)
        { }

        protected virtual void OnPlayerExited(PlayerManager player)
        { }

        protected virtual bool CanInteract(PlayerManager player)
        {
            return player &&
                   player.ControlContext == PlayerControlContext.LobbySandbox &&
                   player.CurrentPermissions.CanInteract;
        }

        protected abstract void Interact(PlayerManager player);

        public bool CanHandleInteraction(PlayerManager player)
        {
            return player &&
                   PlayersInside.Contains(player) &&
                   CanInteract(player);
        }

        public bool HandleInteract(PlayerManager player)
        {
            if (!CanHandleInteraction(player))
                return false;

            Interact(player);
            return true;
        }

        private void UnregisterAllPlayers()
        {
            if (PlayersInside.Count == 0)
                return;

            var players = new List<PlayerManager>(PlayersInside);

            for (var i = 0; i < players.Count; i++)
            {
                InteractionService?.Unregister(players[i], this);
            }
        }

        private static bool TryGetPlayer(Collider other, out PlayerManager player)
        {
            player = null;

            if (!other)
                return false;

            var character = other.GetComponentInParent<PlayerCharacter>();

            if (!character || !character.Owner)
                return false;

            player = character.Owner;
            return true;
        }
    }
}