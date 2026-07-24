using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MortierFu
{
    public sealed class PlayerUIInputService : IGameService
    {
        private readonly Dictionary<PlayerManager, List<IPlayerUIInputHandler>> _handlersByPlayer = new();

        private readonly Dictionary<PlayerManager, Vector2> _navigationInputByPlayer = new();
        private readonly List<PlayerManager> _navigationPlayersBuffer = new();

        private CancellationTokenSource _navigationLoopCancellation;
        
        public bool IsInitialized { get; set; }

        public UniTask OnInitialize()
        {
            _navigationLoopCancellation?.Cancel();
            _navigationLoopCancellation?.Dispose();

            _navigationLoopCancellation = new CancellationTokenSource();
            RunNavigationLoop(_navigationLoopCancellation.Token).Forget();

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

            _navigationInputByPlayer.Remove(player);

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

            if (handlers.Count != 0) return;
            
            _handlersByPlayer.Remove(player);
            _navigationInputByPlayer.Remove(player);
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
            
            if (!_handlersByPlayer.ContainsKey(player))
                return false;
            if (direction.sqrMagnitude < 0.0001f)
            {
                _navigationInputByPlayer.Remove(player);
                TryHandle(player, handler => handler.HandleNavigate(player, Vector2.zero));
                return true;
            }

            _navigationInputByPlayer[player] = direction;
            
            return true;
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
            switch (handler)
            {
                case null:
                case Object unityObject when !unityObject:
                    return true;
                default:
                    return false;
            }
        }
        
        private async UniTaskVoid RunNavigationLoop(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    _navigationPlayersBuffer.Clear();

                    foreach (var pair in _navigationInputByPlayer)
                    {
                        _navigationPlayersBuffer.Add(pair.Key);
                    }

                    for (int i = 0; i < _navigationPlayersBuffer.Count; i++)
                    {
                        PlayerManager player = _navigationPlayersBuffer[i];

                        if (!player || !player.CurrentPermissions.CanNavigateUI)
                        {
                            _navigationInputByPlayer.Remove(player);
                            continue;
                        }

                        if (!_navigationInputByPlayer.TryGetValue(player, out Vector2 input))
                            continue;
                        
                        TryHandle(player, handler => handler.HandleNavigate(player, input));
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            { }
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
            _navigationInputByPlayer.Clear();
        }

        public void Dispose()
        {
            _navigationLoopCancellation?.Cancel();
            _navigationLoopCancellation?.Dispose();
            _navigationLoopCancellation = null;

            ClearAllHandlers();
        }
    }
}