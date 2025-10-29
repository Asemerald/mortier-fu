using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MortierFu
{
    public class LobbyPanel : MonoBehaviour
    {
        [Header("Player Slots Reference")]
        [SerializeField] private GameObject[] playerSlots;
        [SerializeField] private TextMeshProUGUI[] playerSlotTexts;
        [SerializeField] private Button startGameButton;
        [Header("Customization")]
        [SerializeField] private GameObject[] customizationSlots;


        private void Start()
        {
            Hide();
            UpdateSlots(new List<PlayerInput>());
        }

        private void OnEnable()
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }
        
        private void OnDisable()
        {
            startGameButton.onClick.RemoveListener(OnStartGameClicked);
        }

        private void Show()
        {
            gameObject.SetActive(true);
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }

        public void UpdateSlots(List<PlayerInput> joinedPlayers)
        {
            for (var i = 0; i < playerSlots.Length; i++)
            {
                if (i < joinedPlayers.Count)
                {
                    playerSlots[i].SetActive(true);
                    if (playerSlotTexts != null && i < playerSlotTexts.Length && playerSlotTexts[i] != null)
                        playerSlotTexts[i].text = $"Joueur {i + 1}";
                }
                else
                {
                    playerSlots[i].SetActive(false);
                    if (playerSlotTexts != null && i < playerSlotTexts.Length && playerSlotTexts[i] != null)
                        playerSlotTexts[i].text = string.Empty;
                }
            }
            
            //startGameButton.interactable = (joinedPlayers.Count >= 2 && joinedPlayers.Count <= 4);
        }

        private void OnStartGameClicked()
        {
            //LobbyManager.Instance?.TryStartGame();
            SceneManager.LoadScene("GameLoop");
        }
    }
}