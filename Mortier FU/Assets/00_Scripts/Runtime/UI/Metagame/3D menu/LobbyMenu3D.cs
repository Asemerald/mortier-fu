using System;
using Discord.Sdk;
using MortierFu;
using MortierFu.Shared;
using UnityEngine;

public class LobbyMenu3D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] public GameObject[] playerPrefabs;
    
    public static LobbyMenu3D Instance { get; private set; }
    
    private int playerCount = 0;
    
    private LobbyService _lobby;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Logs.LogWarning("[LobbyMenu3D]: Multiple instances detected. Destroying duplicate.", this);
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        
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
        
        _lobby.OnPlayerJoined += HandlePlayerJoined;
        _lobby.OnPlayerLeft += HandlePlayerLeft;
    }
    
    private void OnDisable()
    {
        _lobby.OnPlayerJoined -= HandlePlayerJoined;
        _lobby.OnPlayerLeft -= HandlePlayerLeft;
    }
    
    private void HandlePlayerJoined(PlayerManager playerManager)
    {
        playerCount = _lobby.CurrentPlayerCount;
        RefreshPlayerModels();
    }
    
    private void HandlePlayerLeft(PlayerManager playerManager)
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
