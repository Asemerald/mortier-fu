using System;
using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public abstract class LobbyInteractionZone : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private string _interactActionName = "Interact";

        protected readonly HashSet<PlayerManager> PlayersInside = new();

        private readonly Dictionary<PlayerManager, InputAction> _boundActions = new();
        private readonly Dictionary<PlayerManager, Action<InputAction.CallbackContext>> _boundHandlers = new();

        private void OnTriggerEnter(Collider other)
        {
            if (!TryGetPlayer(other, out var player))
                return;

            if (!PlayersInside.Add(player))
                return;

            BindPlayer(player);
            OnPlayerEntered(player);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!TryGetPlayer(other, out var player))
                return;

            if (!PlayersInside.Remove(player))
                return;

            UnbindPlayer(player);
            OnPlayerExited(player);
        }

        protected virtual void OnDisable()
        {
            UnbindAllPlayers();
            PlayersInside.Clear();
        }

        protected virtual void OnPlayerEntered(PlayerManager player)
        {
        }

        protected virtual void OnPlayerExited(PlayerManager player)
        {
        }

        protected virtual bool CanInteract(PlayerManager player)
        {
            return player != null && player.ControlContext == PlayerControlContext.LobbySandbox;
        }

        protected abstract void Interact(PlayerManager player);

        private void BindPlayer(PlayerManager player)
        {
            if (player == null || player.PlayerInput == null)
                return;

            if (_boundActions.ContainsKey(player))
                return;

            var action = player.PlayerInput.actions.FindAction(_interactActionName, false);

            if (action == null)
            {
                Logs.LogWarning($"[LobbyInteractionZone] Action '{_interactActionName}' not found for Player {player.PlayerIndex + 1}.");
                return;
            }

            Action<InputAction.CallbackContext> handler = ctx =>
            {
                if (!ctx.performed)
                    return;

                if (!PlayersInside.Contains(player))
                    return;

                if (!CanInteract(player))
                    return;

                Interact(player);
            };

            action.performed += handler;

            if (!action.enabled)
                action.Enable();

            _boundActions[player] = action;
            _boundHandlers[player] = handler;
        }

        private void UnbindPlayer(PlayerManager player)
        {
            if (player == null)
                return;

            if (!_boundActions.TryGetValue(player, out var action))
                return;

            if (_boundHandlers.TryGetValue(player, out var handler))
            {
                action.performed -= handler;
            }

            _boundActions.Remove(player);
            _boundHandlers.Remove(player);
        }

        private void UnbindAllPlayers()
        {
            var players = new List<PlayerManager>(_boundActions.Keys);

            foreach (var player in players)
            {
                UnbindPlayer(player);
            }
        }

        private static bool TryGetPlayer(Collider other, out PlayerManager player)
        {
            player = null;

            if (other == null)
                return false;

            var character = other.GetComponentInParent<PlayerCharacter>();

            if (character == null || character.Owner == null)
                return false;

            player = character.Owner;
            return true;
        }
    }
}