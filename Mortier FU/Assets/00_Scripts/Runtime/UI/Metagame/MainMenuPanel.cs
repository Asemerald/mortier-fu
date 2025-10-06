using UnityEngine;

public class MainMenuPanel : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject quitConfirmationPanel;
    
    [Header("Buttons")]
    [SerializeField] private GameObject playButton;
    [SerializeField] private GameObject settingsButton;
    [SerializeField] private GameObject creditsButton;
    [SerializeField] private GameObject quitButton;
    
    private void Start()
    {
        Show();
    }
    
    public void Show()
    {
        mainMenuPanel.SetActive(true);
    }
    
    public void Hide()
    {
        mainMenuPanel.SetActive(false);
    }
}
