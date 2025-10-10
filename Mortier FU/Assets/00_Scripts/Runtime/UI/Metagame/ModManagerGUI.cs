using UnityEngine;

namespace MortierFu
{
    public class ModMenuGUI : MonoBehaviour
    {
        private Vector2 scrollPos;
        private bool showMenu = false;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
                showMenu = !showMenu;
        }

        void OnGUI()
        {
            if (!showMenu) return;

            GUI.Box(new Rect(10, 10, 300, 400), "Mods");

            scrollPos = GUI.BeginScrollView(new Rect(10, 40, 300, 350), scrollPos, new Rect(0, 0, 280, ModManager.Instance.allMods.Count * 30));

            for (int i = 0; i < ModManager.Instance.allMods.Count; i++)
            {
                var mod = ModManager.Instance.allMods[i];
                bool newEnabled = GUI.Toggle(new Rect(10, i * 30, 200, 20), mod.isEnabled, mod.name);
                if (newEnabled != mod.isEnabled)
                {
                    ModManager.Instance.ToggleMod(mod);
                }
            }

            GUI.EndScrollView();

            if (GUI.Button(new Rect(10, 360, 100, 30), "Load Mods"))
            {
                ModManager.Instance.LoadMods();
            }

            if (GUI.Button(new Rect(120, 360, 100, 30), "Restart"))
            {
                ModManager.Instance.RestartGame();
            }
        }
    }
}