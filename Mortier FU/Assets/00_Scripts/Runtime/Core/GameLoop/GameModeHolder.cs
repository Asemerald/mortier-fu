﻿using System;
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
            GameInitializer initializer = FindObjectOfType<GameInitializer>();
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
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.L) && !_initialized)
            {
                _gm.Initialize();
                _gm.StartGame();
                _initialized = true;
            }
        }
#endif
    }
}