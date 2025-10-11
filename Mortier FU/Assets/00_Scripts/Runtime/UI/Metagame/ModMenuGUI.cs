using UnityEngine;
using System.Diagnostics;
using System.IO;

namespace MortierFu
{
    public class ModMenuGUI : MonoBehaviour
    {
        private Vector2 scrollPos;
        private bool showMenu = false;
        private Rect windowRect = new Rect(50, 50, 450, 580);

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
                showMenu = !showMenu;
        }

        void OnGUI()
        {
            if (!showMenu) return;
            windowRect = GUI.Window(0, windowRect, DrawWindow, "Mod Manager");
        }

        void DrawWindow(int id)
        {
            GUILayout.Space(10);
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(430), GUILayout.Height(460));

            foreach (var mod in ModManager.Instance.allMods)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label($"<b>{mod.manifest.name}</b> v{mod.manifest.version}", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 });
                GUILayout.Label($"<i>by {mod.manifest.author}</i>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11 });
                if (!string.IsNullOrEmpty(mod.manifest.description))
                    GUILayout.Label(mod.manifest.description, GUILayout.Width(400));

                bool newEnabled = GUILayout.Toggle(mod.isEnabled, "Enabled");
                if (newEnabled != mod.isEnabled)
                    ModManager.Instance.ToggleMod(mod);

                GUILayout.EndVertical();
                GUILayout.Space(6);
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload Mods", GUILayout.Width(130)))
                ModManager.Instance.ScanMods();
            if (GUILayout.Button("Load Mods", GUILayout.Width(130)))
                ModManager.Instance.LoadMods();
            if (GUILayout.Button("Restart Game", GUILayout.Width(130)))
                ModManager.Instance.RestartGame();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            if (GUILayout.Button("Open Mods Folder", GUILayout.Width(410)))
            {
                string modsPath = Path.Combine(Application.dataPath, "Mods");
                if (Directory.Exists(modsPath))
                    Process.Start(new ProcessStartInfo { FileName = modsPath, UseShellExecute = true });
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 25));
        }
    }
}
