using UnityEditor;
using UnityEngine;
using MortierFU.Shared;

namespace MortierFU.Editor
{
    public class EnterPlayModeToggle
    {
        private const string MenuName = "Tools/Fast Play Mode";
        
        [MenuItem(MenuName, false, 10)] // Ajouter un raccourci Ctrl+Alt+P
        private static void TogglePlayModeSettings()

        {
            var enabled = EditorSettings.enterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptionsEnabled = !enabled;

            if (!enabled)
            {
                EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload |
                                                      EnterPlayModeOptions.DisableSceneReload;
                Logs.Log("Fast Play Mode ACTIVÉ");
            }
            else
            {
                EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.None;
                Logs.Log("Fast Play Mode DÉSACTIVÉ");
            }

            Menu.SetChecked(MenuName, EditorSettings.enterPlayModeOptionsEnabled);
        }

        [MenuItem(MenuName, true)]
        private static bool ValidateMenu()
        {
            Menu.SetChecked(MenuName, EditorSettings.enterPlayModeOptionsEnabled);
            return true;
        }
    }
}