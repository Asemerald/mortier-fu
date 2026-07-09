using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MortierFu
{
    public sealed class PlayerUIInputService : IGameService
    {
        private readonly Dictionary<PlayerManager, List<IPlayerUIInputHandler>> _handlersByPlayer = new();

        public bool IsInitialized { get; set; }

        public UniTask OnInitialize()
        {
            return UniTask.CompletedTask;
        }

        public void Push(PlayerManager player, IPlayerUIInputHandler handler)
        {
            if (!player || handler is null)
                return;

            if (!_handlersByPlayer.TryGetValue(player, out var handlers))
            {
                handlers = new List<IPlayerUIInputHandler>();
                _handlersByPlayer.Add(player, handlers);
            }

            RemoveFromList(handlers, handler);
            handlers.Add(handler);
        }

        public void Remove(PlayerManager player, IPlayerUIInputHandler handler)
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

        public void RemoveFromAll(IPlayerUIInputHandler handler)
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

        public bool TryNavigate(PlayerManager player, Vector2 direction)
        {
            if (!player || !player.CurrentPermissions.CanNavigateUI)
                return false;

            return TryHandle(player, handler => handler.HandleNavigate(player, direction));
        }

        public bool TrySubmit(PlayerManager player)
        {
            if (!player || !player.CurrentPermissions.CanConfirmUI)
                return false;
            
            return TryHandle(player, handler => handler.HandleSubmit(player));
        }

        public bool TryCancel(PlayerManager player)
        {
            if (!player || !player.CurrentPermissions.CanCancelUI)
                return false;

            return TryHandle(player, handler => handler.HandleCancel(player));
        }

        private bool TryHandle(PlayerManager player, Func<IPlayerUIInputHandler, bool> dispatch)
         {
            if (!player || dispatch is null)
                return false;
            
            if (!_handlersByPlayer.TryGetValue(player, out var handlers))
            {
                // stoian added for Race when spamming A when already accept : feedback
                ServiceManager.Instance.Get<ShakeService>()?.ShakeController(player, ShakeService.ShakeType.LITTLE);
                
                return false;
            }
            
            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                var handler = handlers[i];

                if (IsInvalidHandler(handler))
                {
                    handlers.RemoveAt(i);
                    continue;
                }

                if (!handler.CanHandleUIInput(player))
                    continue;

                return dispatch(handler);
            }

            return false;
        }

        private static bool IsInvalidHandler(IPlayerUIInputHandler handler)
        {
            if (handler is null)
                return true;

            if (handler is Object unityObject && !unityObject)
                return true;

            return false;
        }

        private static void RemoveFromList(List<IPlayerUIInputHandler> handlers, IPlayerUIInputHandler handler)
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
        
        public void ClearAllHandlers()
        {
            _handlersByPlayer.Clear();
        }

        public void Dispose()
        {
            ClearAllHandlers();
        }
    }
}