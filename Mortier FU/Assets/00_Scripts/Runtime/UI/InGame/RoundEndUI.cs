using MortierFu;
using MortierFu.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.ObjectModel;

public class RoundEndUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image winnerImage;
    [SerializeField] private Image winnerImageBackground;
    [SerializeField] private Image[] playerImages;
    [SerializeField] private TMP_Text[] scoreTexts;

    [Header("Assets")] 
    [SerializeField] private Material[] winnerMaterials;
    [SerializeField] private Material[] winnerBackgroundMaterial;
    [SerializeField] private Sprite[] defaultSprites;
    [SerializeField] private Sprite[] winnerSprites;

    private GameModeBase _gm;

    private void Start()
    {
        HideScore();
        
        _gm = GameService.CurrentGameMode as GameModeBase;
        if (_gm == null)
        {
            Logs.LogWarning("Game mode not found");
            return;
        }

        _gm.OnRoundEnded += DisplayScores;
        _gm.OnScoreDisplayOver += HideScore;
    }
    
    private void DisplayScores(RoundInfo round)
    {
        var lobbyService = ServiceManager.Instance.Get<LobbyService>();
        int playerCount = lobbyService.GetPlayers().Count;
        DisplayPlayerImages(playerCount, _gm.Teams);
        
        SetWinner(round.WinningTeam.Index);
    }

    private void DisplayPlayerImages(int playerCount, ReadOnlyCollection<PlayerTeam> playerTeams)
    {
        for (int i = 0; i < playerTeams.Count; i++)
        {
            if (i < playerCount)
            {
                playerImages[i].gameObject.SetActive(true);
                playerImages[i].sprite = defaultSprites[playerTeams[i].Index];
                scoreTexts[i].text = playerTeams[i].Score.ToString();
            }
            else
            {
                playerImages[i].gameObject.SetActive(false);
                scoreTexts[i].gameObject.SetActive(false);
            }
        }
        
        // Set Player Image to winning team sprite if team has the most point 
        int highestScore = -1;
        int winningTeamIndex = -1;
        foreach (var t in playerTeams)
        {
            if (t.Score > highestScore)
            {
                highestScore = t.Score;
                winningTeamIndex = t.Index;
            }
        }
        
        for (int i = 0; i < playerTeams.Count; i++)
        {
            if (i < playerCount)
            {
                if (playerTeams[i].Index == winningTeamIndex)
                {
                    playerImages[i].sprite = winnerSprites[playerTeams[i].Index];
                }
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

    private void HideScore()
    {
        winnerImage.gameObject.SetActive(false);
        winnerImageBackground.gameObject.SetActive(false);

        foreach (var t in playerImages)
        {
            t.gameObject.SetActive(false);
        }
    }
    
    
}
