using System;
using MortierFu;
using MortierFu.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoundEndUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image winnerImage;
    [SerializeField] private Image winnerImageBackground;
    [SerializeField] private Image[] playerImages;
    [SerializeField] private Image[] scoreBackgroundImages;
    [SerializeField] private TMP_Text[] scoreTexts;

    [Header("Assets")] 
    [SerializeField] private Material[] winnerMaterials;
    [SerializeField] private Material[] winnerBackgroundMaterial;
    [SerializeField] private Sprite[] winnerSprites;

    private GameModeBase _gm;

    private void Start()
    {
        _gm = GameService.CurrentGameMode as GameModeBase;
        if (_gm == null)
        {
            Logs.LogWarning("Game mode not found");
            return;
        }

        _gm.OnGameEnded += DisplayVictoryScreen;
    }
    
    private void DisplayVictoryScreen(int WinnerIndex)
    {
        var lobbyService = ServiceManager.Instance.Get<LobbyService>();
        int playerCount = lobbyService.GetPlayers().Count;
        DisplayPlayerImages(playerCount, WinnerIndex);
        
        SetWinner(WinnerIndex);
    }

    private void DisplayPlayerImages(int playerCount, int winnerIndex)
    {
        for (int i = 0; i < playerImages.Length; i++)
        {
            if (i < playerCount)
            {
                playerImages[i].gameObject.SetActive(true);
            }
            else
            {
                playerImages[i].gameObject.SetActive(false);
            }
        }
        
        // Change Sprites for winner 
        for (int i = 0; i < playerCount; i++)
        {
            if (i == winnerIndex)
            {
                playerImages[i].sprite = winnerSprites[i];
            }
        }
    }
    
    

    private void SetWinner(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < winnerMaterials.Length)
        {
            winnerImage.material = winnerMaterials[playerIndex];
            winnerImageBackground.material = winnerBackgroundMaterial[playerIndex];
            winnerImage.gameObject.SetActive(true);
            winnerImageBackground.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("Invalid PlayerIndex");
        }
    }

    
    
    
}
