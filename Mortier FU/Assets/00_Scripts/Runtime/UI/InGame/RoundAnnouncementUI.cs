using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MortierFu
{
    public class RoundAnnouncementUI : MonoBehaviour
    {
        [SerializeField] private GameObject _goldenBombshellGameObject;

        private GameModeBase _gm;
        
        public void OnGameStarted()
        {
            _gm = GameService.CurrentGameMode as GameModeBase;
            
            _gm.OnGameStarted -= OnGameStarted;
            InitializeUI();
        }

        public void OnRoundStarted()
        {
            UpdateMatchPointIndicator();
        }

        private void InitializeUI()
        {
            gameObject.SetActive(false);

            _goldenBombshellGameObject.SetActive(false);
        }

        private void UpdateMatchPointIndicator()
        {
            if (_gm == null || _goldenBombshellGameObject.activeSelf) return;

            bool isMatchPoint = false;

            for (int i = 0; i < _gm.Teams.Count; i++)
            {
                if (_gm.Teams[i].Score < _gm.Data.ScoreToWin) continue;
                isMatchPoint = true;
                break;
            }

            _goldenBombshellGameObject.SetActive(isMatchPoint);
        }
    }
}