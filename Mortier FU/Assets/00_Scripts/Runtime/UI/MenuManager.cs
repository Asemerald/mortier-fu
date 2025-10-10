using System;
using MortierFu;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private MainMenuPanel mainMenuPanel;
    [SerializeField] private SettingsPanel settingsPanel;
    [SerializeField] private CreditsPanel creditsPanel;
    [SerializeField] private LobbyPanel lobbyPanel;
    
    private void Awake()
    {
        CheckReferences();
    }
    
    private void CheckReferences()
    {
        if (mainMenuPanel == null)
        {
            Logs.LogError("MenuManager: MainMenuPanel reference is missing.", this);
        }
        if (settingsPanel == null)
        {
            Logs.LogError("MenuManager: SettingsPanel reference is missing.", this);
        }
        if (creditsPanel == null)
        {
            Logs.LogError("MenuManager: CreditsPanel reference is missing.", this);
        }
        if (lobbyPanel == null)
        {
            Logs.LogError("MenuManager: LobbyPanel reference is missing.", this);
        }
    }
}
