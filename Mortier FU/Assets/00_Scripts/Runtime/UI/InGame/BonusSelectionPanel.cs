using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public class BonusSelectionPanel : MonoBehaviour
    {
        [SerializeField] private Transform bonusPanelGroup;
        [SerializeField] private GameObject bonusButtonPrefab;
        [SerializeField] private GameObject playerLabelPrefab;

        private Dictionary<PlayerInput, Button> playerBonusButtons = new();
        private Dictionary<PlayerInput, string> playerBonusChoices = new();
        private List<PlayerInput> currentPlayers;
        private List<string> currentBonuses;

        public event System.Action OnAllPlayersSelected;

        public void Init(List<PlayerInput> players, List<string> bonusNames)
        {
            foreach (Transform child in bonusPanelGroup)
                Destroy(child.gameObject);
            playerBonusButtons.Clear();
            playerBonusChoices.Clear();
            currentPlayers = players;
            currentBonuses = bonusNames;

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                var labelGo = Instantiate(playerLabelPrefab, bonusPanelGroup);
                var label = labelGo.GetComponent<TMP_Text>();
                label.text = $"Joueur {player.playerIndex + 1}";

                var buttonGo = Instantiate(bonusButtonPrefab, bonusPanelGroup);
                var button = buttonGo.GetComponent<Button>();
                var buttonText = buttonGo.GetComponentInChildren<TMP_Text>();
                string bonusName = bonusNames[i % bonusNames.Count];
                buttonText.text = bonusName;
                playerBonusButtons[player] = button;
                
                button.onClick.AddListener(() => OnBonusSelected(player, bonusName));
            }
            Hide();
        }

        private void OnBonusSelected(PlayerInput player, string bonusName)
        {
            playerBonusChoices[player] = bonusName;
            if (playerBonusButtons.TryGetValue(player, out var btn))
                btn.interactable = false;
            
            if (AllPlayersSelected())
                OnAllPlayersSelected?.Invoke();
        }

        public string GetPlayerBonus(PlayerInput player)
        {
            return playerBonusChoices.TryGetValue(player, out var bonus) ? bonus : null;
        }

        private bool AllPlayersSelected()
        {
            return playerBonusChoices.Count == currentPlayers.Count;
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
