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

        private Dictionary<Button, PlayerInput> buttonToPlayer = new();
        private Dictionary<PlayerInput, string> playerBonusChoices = new();
        private List<Button> spawnedButtons = new();

        public event System.Action OnAllPlayersSelected;

        public void Init(List<Cnc> players, List<string> bonusNames)
        {
            foreach (Transform child in bonusPanelGroup)
                Destroy(child.gameObject);
            buttonToPlayer.Clear();
            playerBonusChoices.Clear();
            spawnedButtons.Clear();

            for (int i = 0; i < bonusNames.Count; i++)
            {
                var buttonGo = Instantiate(bonusButtonPrefab, bonusPanelGroup);
                var button = buttonGo.GetComponent<Button>();
                var buttonText = buttonGo.GetComponentInChildren<TMP_Text>();
                buttonText.text = bonusNames[i];
                spawnedButtons.Add(button);
                int idx = i;
                button.onClick.AddListener(() => OnBonusSelected(button, bonusNames[idx], players));
            }
            Hide();
        }

        private void OnBonusSelected(Button button, string bonusName, List<Cnc> players)
        {
            PlayerInput selectingPlayer = null;
            foreach (var player in players)
            {
                if (!playerBonusChoices.ContainsKey(player.GameInput))
                {
                    selectingPlayer = player.GameInput;
                    break;
                }
            }
            if (selectingPlayer == null) return;
            
            playerBonusChoices[selectingPlayer] = bonusName;
            buttonToPlayer[button] = selectingPlayer;
            
            var buttonText = button.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = $"Joueur {selectingPlayer.playerIndex + 1}";
            }
            button.interactable = false;
            
            if (playerBonusChoices.Count == players.Count)
            {
                OnAllPlayersSelected?.Invoke();
            }
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
