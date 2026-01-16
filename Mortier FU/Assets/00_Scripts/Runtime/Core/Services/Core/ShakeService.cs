using System;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;


namespace MortierFu
{
    public class ShakeService : IGameService
    {
        private LobbyService _lobbyService;   
        
        public enum ShakeType
        {
            LITTLE,
            MID,
            BIG
        }

        public void ShakeController(PlayerManager pm, ShakeType type)
        {
            Gamepad gamepad = pm.PlayerInput.GetDevice<Gamepad>();

            switch (type)
            {
                case ShakeType.LITTLE:
                    Shake(gamepad, 0.12f, 0.08f);
                    break;
                case ShakeType.MID:
                    Shake(gamepad, 0.45f, 0.17f);
                    break;
                case ShakeType.BIG:
                    Shake(gamepad, 0.8f, 0.25f);
                    break;
            }
        }

        public void ShakeController(InputDevice pm, ShakeType type)
        {
            Gamepad gamepad = pm as Gamepad;

            switch (type)
            {
                case ShakeType.LITTLE:
                    Shake(gamepad, 0.12f, 0.08f);
                    break;
                case ShakeType.MID:
                    Shake(gamepad, 0.45f, 0.17f);
                    break;
                case ShakeType.BIG:
                    Shake(gamepad, 0.8f, 0.25f);
                    break;
            }
        }

        public void ShakeControllers(ShakeType type)
        {
            foreach (var playerManager in _lobbyService.Players)
            {
                Gamepad gamepad = playerManager.PlayerInput.GetDevice<Gamepad>();
                
                switch (type)
                {
                    case ShakeType.LITTLE:
                        Shake(gamepad, 0.12f, 0.08f);
                        break;
                    case ShakeType.MID:
                        Shake(gamepad, 0.45f, 0.17f);
                        break;
                    case ShakeType.BIG:
                        Shake(gamepad, 0.8f, 0.25f);
                        break;
                }
            }
        }

        private async UniTask Shake(Gamepad gamepad, float intensity, float time)
        {
            gamepad.SetMotorSpeeds(intensity, intensity);
            await UniTask.Delay(TimeSpan.FromSeconds(time), ignoreTimeScale: true);
            gamepad.SetMotorSpeeds(0, 0);
        }

        public void Dispose()
        {
        }

        public UniTask OnInitialize()
        {
            _lobbyService = ServiceManager.Instance.Get<LobbyService>();
            return UniTask.CompletedTask;
        }

        public bool IsInitialized { get; set; }
    }
}