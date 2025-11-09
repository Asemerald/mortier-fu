using System;
using System.Threading.Tasks;
using UnityEngine;

namespace MortierFu
{
    public class GameModeHolder : MonoBehaviour
    {
        [SerializeField] private SO_GameModeData gameModeData;
        private GameModeBase _gm;

        async void Awake()
        {
            _gm = new GM_FFA();
#if UNITY_EDITOR
            GameInitializer initializer = FindFirstObjectByType<GameInitializer>();
            if (initializer != null && initializer.isPortableBootstrap) return;
#endif
            _gm.Initialize();
            await Task.Delay(TimeSpan.FromSeconds(1));
            _gm.StartGame();
        }

#if UNITY_EDITOR
        bool _initialized = false;
        private async void Update()
        {
            if (Input.GetKeyDown(KeyCode.L) && !_initialized)
            {
                SystemManager.Instance.CreateAndRegister<AugmentSelectionSystem>();
                SystemManager.Instance.CreateAndRegister<BombshellSystem>();
                SystemManager.Instance.CreateAndRegister<LevelSystem>();
                await SystemManager.Instance.Initialize();
                
                _gm.Initialize();
                _gm.StartGame();
                _initialized = true;
            }
        }
#endif
    }
}