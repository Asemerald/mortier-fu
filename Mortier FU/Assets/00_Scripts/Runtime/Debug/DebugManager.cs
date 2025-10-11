using System.Diagnostics;
using System.IO;
using MortierFu.Shared;
using UnityEngine;

public class DebugManager : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
#if !UNITY_EDITOR
        // if I press Ctrl+R, restart the game
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
#endif
    }
    
    public static void RestartGame()
    {
        string exePath = Application.dataPath; // normalement le dossier Data
        string parentPath = Path.GetFullPath(Path.Combine(exePath, ".."));
        string gameExe = Path.Combine(parentPath, Path.GetFileNameWithoutExtension(Application.dataPath) + ".exe");

        if (!File.Exists(gameExe))
        {
            Logs.LogError($"Restart failed: game exe not found at {gameExe}");
            return;
        }

        try
        {
            Process.Start(gameExe);
            UnityEngine.Debug.Log("Restarting game...");
            Application.Quit();
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Restart failed: {e.Message}");
        }
    }
}
