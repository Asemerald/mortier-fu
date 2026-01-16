using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;


namespace MortierFu
{
    public class ShakeService : IGameService
    {
        public enum ShakeType
        {
            LITTLE, MID, BIG
        }
        
        public static void ShakeController(PlayerManager pm, ShakeType type)
        {
            Gamepad gamepad = pm.PlayerInput.GetDevice<Gamepad>();

            switch (type)
            {
                case ShakeType.LITTLE :
                    Shake(gamepad, 0.12f, 0.08f);
                    break;
                case ShakeType.MID :
                    Shake(gamepad, 0.45f, 0.17f);
                    break;
                case ShakeType.BIG :
                    Shake(gamepad, 0.8f, 0.25f);
                    break;
            }
        }

        private static async UniTask Shake(Gamepad gamepad, float intensity, float time)
        {
            gamepad.SetMotorSpeeds(intensity, intensity);
            await UniTask.Delay(TimeSpan.FromSeconds(time));
            gamepad.SetMotorSpeeds(0, 0);
        }
        
        public void Dispose()
        {
            
        }
        public UniTask OnInitialize()
        {
            return UniTask.CompletedTask;
        }

        public bool IsInitialized { get; set; }
    }
}

