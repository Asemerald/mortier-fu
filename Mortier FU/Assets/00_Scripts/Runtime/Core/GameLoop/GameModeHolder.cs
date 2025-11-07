using UnityEngine;

namespace MortierFu
{
    public class GameModeHolder : MonoBehaviour
    {
        [SerializeField] private SO_GameModeData gameModeData;
        private GameModeBase _gm;

        void Start()
        {
            _gm = new GM_FFA();
            _gm.GameModeData = gameModeData;
#if UNITY_EDITOR
            GameInitializer initializer = FindFirstObjectByType<GameInitializer>();
            if (initializer != null && initializer.isPortableBootstrap) return;
#endif
            _gm.Initialize();
            _gm.StartGame();
        }

        public GameModeBase Get()
        {
            return _gm;
        }
        
#if UNITY_EDITOR
        bool _initialized = false;
        private async void Update()
        {
            if (Input.GetKeyDown(KeyCode.L) && !_initialized)
            {
                SystemManager.Instance.CreateAndRegister<AugmentSelectionSystem>();
                SystemManager.Instance.CreateAndRegister<BombshellSystem>();
                await SystemManager.Instance.Initialize();
                
                _gm.Initialize();
                _gm.StartGame();
                _initialized = true;
            }
        }
#endif
    }
}