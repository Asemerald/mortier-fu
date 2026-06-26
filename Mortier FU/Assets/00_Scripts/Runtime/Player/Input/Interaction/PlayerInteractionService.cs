using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MortierFu
{
    public sealed class PlayerInteractionService : IGameService
    {
        private readonly Dictionary<PlayerManager, List<IPlayerInteractionHandler>> _handlersByPlayer = new();

        public bool IsInitialized { get; set; }

        public UniTask OnInitialize()
        {
            return UniTask.CompletedTask;
        }

        public void Register(PlayerManager player, IPlayerInteractionHandler handler)
        {
            if (!player || handler is null)
                return;

            if (!_handlersByPlayer.TryGetValue(player, out var handlers))
            {
                handlers = new List<IPlayerInteractionHandler>();
                _handlersByPlayer.Add(player, handlers);
            }

            RemoveFromList(handlers, handler);
            handlers.Add(handler);
        }

        public void Unregister(PlayerManager player, IPlayerInteractionHandler handler)
        {
            if (!player || handler is null)
                return;

            if (!_handlersByPlayer.TryGetValue(player, out var handlers))
                return;

            RemoveFromList(handlers, handler);

            if (handlers.Count == 0)
            {
                _handlersByPlayer.Remove(player);
            }
        }

        public void UnregisterFromAll(IPlayerInteractionHandler handler)
        {
            if (handler is null)
                return;

            var playersToRemove = new List<PlayerManager>();

            foreach (var pair in _handlersByPlayer)
            {
                var handlers = pair.Value;

                RemoveFromList(handlers, handler);

                if (handlers.Count == 0)
                {
                    playersToRemove.Add(pair.Key);
                }
            }

            for (int i = 0; i < playersToRemove.Count; i++)
            {
                _handlersByPlayer.Remove(playersToRemove[i]);
            }
        }

        public bool TryInteract(PlayerManager player)
        {
            if (!player || !player.CurrentPermissions.CanInteract)
                return false;

            if (!_handlersByPlayer.TryGetValue(player, out var handlers))
                return false;

            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                var handler = handlers[i];

                if (IsInvalidHandler(handler))
                {
                    handlers.RemoveAt(i);
                    continue;
                }

                if (!handler.CanHandleInteraction(player))
                    continue;

                return handler.HandleInteract(player);
            }

            return false;
        }

        private static bool IsInvalidHandler(IPlayerInteractionHandler handler)
        {
            if (handler is null)
                return true;

            if (handler is Object unityObject && !unityObject)
                return true;

            return false;
        }

        private static void RemoveFromList(List<IPlayerInteractionHandler> handlers, IPlayerInteractionHandler handler)
        {
            if (handlers is null || handler is null)
                return;

            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(handlers[i], handler))
                {
                    handlers.RemoveAt(i);
                }
            }
        }

        public void Dispose()
        {
            _handlersByPlayer.Clear();
        }
    }
}