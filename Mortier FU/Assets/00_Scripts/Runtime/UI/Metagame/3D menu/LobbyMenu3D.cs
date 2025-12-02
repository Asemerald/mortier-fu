using UnityEngine;

public class LobbyMenu3D : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    
    private Transform[] playerPrefabSpawns;
    private GameObject[] playerPrefabs;
    
    private int playerCount = 0;

    public void AddPlayer()
    {
        if (playerCount >= playerPrefabSpawns.Length)
            return;
        
        GameObject newPlayer = Instantiate(playerPrefab, playerPrefabSpawns[playerCount].position, playerPrefabSpawns[playerCount].rotation);
        playerPrefabs[playerCount] = newPlayer;
        playerCount++;
    }
    
    public void RemovePlayer()
    {
        if (playerCount <= 0)
            return;
        
        playerCount--;
        Destroy(playerPrefabs[playerCount]);
        playerPrefabs[playerCount] = null;
    }
    
}
