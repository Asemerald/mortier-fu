using UnityEngine;
using System.Collections.Generic;

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
    }
}