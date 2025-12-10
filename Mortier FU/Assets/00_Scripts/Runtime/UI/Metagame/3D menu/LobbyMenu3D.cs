using System;
using MortierFu;
using MortierFu.Shared;
using UnityEngine;

public class LobbyMenu3D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject[] playerPrefabs;
    
    private int playerCount = 0;
    
    private LobbyService _lobby;

    private void Awake()
    {
        _lobby = ServiceManager.Instance.Get<LobbyService>();
        if (_lobby == null)
        {
            Logs.LogError("[LobbyMenu3D]: LobbyService could not be found in ServiceManager.", this);
            return;
        }
    }

    private void OnEnable()
    {
        playerCount = _lobby.CurrentPlayerCount;
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
