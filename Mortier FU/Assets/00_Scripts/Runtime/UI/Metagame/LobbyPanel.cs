using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

        private GameService _gameService;
        
        void Awake()
        {
            // Resolve dependencies
            _gameService = ServiceManager.Instance.Get<GameService>();
        }
        
        private void Start()
        {
            Hide();
            UpdateSlots(new List<PlayerInput>());
            
#if UNITY_EDITOR
            if (EditorPrefs.GetBool("SkipMenuEnabled", false)) {
                StartGame().Forget();
            }
#endif
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

        private void OnStartGameClicked() => StartGame().Forget();
        
        private async UniTask StartGame()
        {
            // When game mode is selected
            await _gameService.InitializeGameMode<GM_FFA>();
            
            // Should handle game mode teams

            _gameService.ExecuteGameplayPipeline().Forget();
        }
    }
}