using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine.InputSystem;
using UnityEngine;
using TMPro;

namespace MortierFu
{
    public class ScorePanel : MonoBehaviour
    {
        [SerializeField] private Transform scorePanelGroup;
        [SerializeField] private GameObject scoreTextPrefab;
        
        private Dictionary<Cnc, TMP_Text> playerScoreTexts = new();
        
        public void Init(List<Cnc> players)
        {
            playerScoreTexts.Clear();

            foreach (Transform child in scorePanelGroup)
            {
                Destroy(child.gameObject);
            }

            foreach (var player in players)
            {
                var go = Instantiate(scoreTextPrefab, scorePanelGroup);
                var text = go.GetComponent<TMP_Text>();
                
                playerScoreTexts[player] = text;
                UpdateScore(player);
            }
            
            Hide();
        }

        private void UpdateScore(Cnc player)
        {
            if (playerScoreTexts.TryGetValue(player, out var scoreText))
            {
                scoreText.text = $"Joueur {player.PlayerNumber + 1} : {player.Score}";
            }
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
