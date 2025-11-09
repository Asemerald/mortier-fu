using UnityEngine;
using UnityEngine.UI;

public class MainMenuPanel : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject quitConfirmationPanel;
    
    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private GameObject settingsButton;
    [SerializeField] private GameObject creditsButton;
    [SerializeField] private GameObject quitButton;
    
    private void Start()
    {
        Show();
        InitializeButtons();
    }

    private void InitializeButtons()
    {
        playButton.onClick.AddListener(OpenLobbyPanel);
    }
    
    public void Show()
    {
        mainMenuPanel.SetActive(true);
    }
    
    public void Hide()
    {
        mainMenuPanel.SetActive(false);
    }

    private void OpenLobbyPanel()
    {
        Hide();
        lobbyPanel.SetActive(true);
    }
}
