using UnityEngine;

public class LobbyMenu3D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject[] playerPrefabs;
    
    private int playerCount = 0;

    public void UpdatePlayersCount(int count)
    {
        playerCount = count;
        RefreshPlayerModels();
    }
    
    private void RefreshPlayerModels()
    {
        for (int i = 0; i < playerPrefabs.Length; i++)
        {
            if (i < playerCount)
            {
                playerPrefabs[i].SetActive(true);
            }
            else
            {
                playerPrefabs[i].SetActive(false);
            }
        }
    }
    
}
