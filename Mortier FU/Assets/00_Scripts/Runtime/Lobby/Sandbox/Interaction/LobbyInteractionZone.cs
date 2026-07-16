using System.Collections.Generic;
using UnityEngine;

namespace MortierFu
{
    public abstract class LobbyInteractionZone : MonoBehaviour, IPlayerInteractionHandler
    {
        private readonly HashSet<PlayerManager> _playersInside = new();

        private PlayerInteractionService InteractionService => ServiceManager.Instance?.Get<PlayerInteractionService>();

        private void OnTriggerEnter(Collider other)
        {
            if (!TryGetPlayer(other, out var player))
                return;

            if (!_playersInside.Add(player) || InteractionService == null)
                return;

            InteractionService.Register(player, this);

            OnPlayerEntered(player);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!TryGetPlayer(other, out var player))
                return;

            if (!_playersInside.Remove(player) || InteractionService == null)
                return;
            
            InteractionService.Unregister(player, this);

            OnPlayerExited(player);
        }

        protected virtual void OnDisable()
        {
            UnregisterAllPlayers();
            _playersInside.Clear();
        }

        protected virtual void OnPlayerEntered(PlayerManager player)
        { }

        protected virtual void OnPlayerExited(PlayerManager player)
        { }

        protected virtual bool CanInteract(PlayerManager player) => player && player.ControlContext == PlayerControlContext.LobbySandbox && player.CurrentPermissions.CanInteract;

        protected abstract void Interact(PlayerManager player);

        public bool CanHandleInteraction(PlayerManager player) => player && _playersInside.Contains(player) && CanInteract(player);

        public bool HandleInteract(PlayerManager player)
        {
            if (!CanHandleInteraction(player))
                return false;

            Interact(player);
            return true;
        }

        private void UnregisterAllPlayers()
        {
            if (_playersInside.Count == 0)
                return;

            var players = new List<PlayerManager>(_playersInside);

            if(InteractionService == null)
                return;
            
            foreach (var player in players)
                InteractionService.Unregister(player, this);
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