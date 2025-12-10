using System;
using MortierFu;
using UnityEngine;
using UnityEngine.UI;

public class GameEndUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image winnerImage;
    [SerializeField] private Image[] playerImages;

    //TODO: Rework et pas hardcoder les couleur mais plutot utiliser les PlayerIndex et une liste de materiaux
    [Header("Assets")] 
    [SerializeField] private Material redWinnerMaterial;
    [SerializeField] private Material blueWinnerMaterial;
    [SerializeField] private Material greenWinnerMaterial;
    [SerializeField] private Material yellowWinnerMaterial;

    private void OnEnable()
    {
        var lobbyService = ServiceManager.Instance.Get<LobbyService>();
        int playerCount = lobbyService.GetPlayers().Count;
        DisplayPlayerImages(playerCount);

        var gameService = ServiceManager.Instance.Get<GameService>();
        int winnerIndex = gameService.GetWinnerPlayerIndex();
        SetWinner(winnerIndex);
    }

    private void DisplayPlayerImages(int playerCount)
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
    }
    
    private void SetWinner(int playerIndex)
    {
        switch (playerIndex)
        {
            case 0:
                winnerImage.material = blueWinnerMaterial;
                break;
            case 1:
                winnerImage.material = redWinnerMaterial;
                break;
            case 2:
                winnerImage.material = greenWinnerMaterial;
                break;
            case 3:
                winnerImage.material = yellowWinnerMaterial;
                break;
            default:
                Debug.LogError("Invalid PlayerIndex");
                break;
        }
    }
    
    
    
}
