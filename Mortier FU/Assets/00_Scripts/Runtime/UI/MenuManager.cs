using System;
using MortierFu;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [field: SerializeField] public MainMenuPanel MainMenuPanel { get; private set; }
    [field: SerializeField] public SettingsPanel SettingsPanel { get; private set; }
    [field: SerializeField] public CreditsPanel CreditsPanel { get; private set; }
    [field: SerializeField] public LobbyPanel LobbyPanel { get; private set; }
    
    private void Awake()
    {
        CheckReferences();
    }
    
    private void CheckReferences()
    {
        if (MainMenuPanel == null)
        {
            Logs.LogError("MenuManager: MainMenuPanel reference is missing.", this);
        }
        if (SettingsPanel == null)
        {
            Logs.LogError("MenuManager: SettingsPanel reference is missing.", this);
        }
        if (CreditsPanel == null)
        {
            Logs.LogError("MenuManager: CreditsPanel reference is missing.", this);
        }
        if (LobbyPanel == null)
        {
            Logs.LogError("MenuManager: LobbyPanel reference is missing.", this);
        }
    }
}
