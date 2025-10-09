using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using TMPro;

namespace MortierFu
{
    public class ScorePanel : MonoBehaviour
    {
        [SerializeField] private Transform scorePanelGroup;
        [SerializeField] private GameObject scoreTextPrefab;
        
        private Dictionary<PlayerInput, TMP_Text> playerScoreTexts = new();
        
        public void Init(List<PlayerInput> players)
        {
            foreach (var player in players)
            {
                var go = Instantiate(scoreTextPrefab, scorePanelGroup);
                var text = go.GetComponent<TMP_Text>();
                
                playerScoreTexts[player] = text;
                UpdateScore(player);
            }
            
            Hide();
        }

        private void UpdateScore(PlayerInput player)
        {
            if (!playerScoreTexts.ContainsKey(player)) 
                return;
            
            var score = GM_Base.Instance.GetPlayerScore(player);
            playerScoreTexts[player].text = $"Joueur {player.playerIndex + 1} : {score}";
        }

        public void UpdateAllScores()
        {
            foreach (var player in playerScoreTexts.Keys)
                UpdateScore(player);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
