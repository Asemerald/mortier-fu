using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

namespace Mortierfu
{
    public class ModManagerGUI : MonoBehaviour
    {
        private bool showWindow = false;
        private Vector2 scroll;
        private ModManager manager;

        void Start()
        {
            manager = ModManager.Instance;
            if (manager == null)
            {
                Debug.LogError("ModManager instance not found!");
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
                showWindow = !showWindow;
        }

        void OnGUI()
        {
            if (!showWindow || manager == null) return;

            float width = 500;
            float height = 400;
            Rect rect = new Rect(20, 20, width, height);
            GUI.Box(rect, "Mods Manager");

            GUILayout.BeginArea(new Rect(30, 50, width - 20, height - 60));
            scroll = GUILayout.BeginScrollView(scroll);

            foreach (var mod in manager.allMods)
            {
                GUILayout.BeginHorizontal("box");

                GUILayout.Label(mod.name, GUILayout.Width(200));
                GUILayout.Label($"v{mod.version}", GUILayout.Width(60));

                string status = mod.isEnabled ? "Enabled" : "Disabled";
                Color color = mod.isEnabled ? Color.green : Color.gray;
                GUI.contentColor = color;
                GUILayout.Label(status, GUILayout.Width(100));
                GUI.contentColor = Color.white;

                bool toggle = GUILayout.Button(mod.isEnabled ? "Disable" : "Enable", GUILayout.Width(80));
                if (toggle)
                {
                    manager.ToggleMod(mod);
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        
        public void RestartGame()
        {
            string exePath = Application.dataPath; // normalement le dossier Data
            string parentPath = Path.GetFullPath(Path.Combine(exePath, ".."));
            string gameExe = Path.Combine(parentPath, Path.GetFileNameWithoutExtension(Application.dataPath) + ".exe");

            if (!File.Exists(gameExe))
            {
                UnityEngine.Debug.LogError($"Restart failed: game exe not found at {gameExe}");
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
}