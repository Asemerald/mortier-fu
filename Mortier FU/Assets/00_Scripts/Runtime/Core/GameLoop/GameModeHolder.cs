using UnityEngine;

namespace MortierFu
{
    public class GameModeHolder : MonoBehaviour
    {
        [SerializeField] private SO_GameModeData gameModeData;
        private GameModeBase _gm;

        void Awake()
        {
            _gm = new GM_FFA();
            _gm.GameModeData = gameModeData;
            _gm.Initialize();
            _gm.StartGame();
        }

        public GameModeBase Get()
        {
            return _gm;
        }
    }
}