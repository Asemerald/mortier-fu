namespace MortierFu
{
    public sealed class MainMenuPanel : UIPanel
    {
        public void OpenSettingsPanel()
        {
            MenuManager.Instance?.OpenSettingsPanel();
        }

        public void OpenCreditsPanel()
        {
            MenuManager.Instance?.OpenCreditsPanel();
        }

        public void QuitGame()
        {
            MenuManager.Instance?.QuitGame();
        }
    }
}