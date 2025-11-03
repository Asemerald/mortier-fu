using UnityEditor;
using UnityEngine;

namespace MortierFu.Shared.Editor
{
    [InitializeOnLoad]
    public static class LogsMenu
    {
        private const string MENU_ROOT = "DEBUG/Log Level/";
        private const string PREF_KEY = "MortierFu.LogLevel";

        static LogsMenu()
        {
            // Charger le niveau de log depuis EditorPrefs
            int savedLevel = EditorPrefs.GetInt(PREF_KEY, (int)Logs.LogLevel.Log);
            Logs.CurrentLevel = (Logs.LogLevel)savedLevel;
            UpdateMenuChecks();
        }

        [MenuItem(MENU_ROOT + "None", false, 0)]
        public static void SetNone() => SetLogLevel(Logs.LogLevel.None);

        [MenuItem(MENU_ROOT + "Error", false, 1)]
        public static void SetError() => SetLogLevel(Logs.LogLevel.Error);

        [MenuItem(MENU_ROOT + "Warning", false, 2)]
        public static void SetWarning() => SetLogLevel(Logs.LogLevel.Warning);

        [MenuItem(MENU_ROOT + "Log", false, 3)]
        public static void SetInfo() => SetLogLevel(Logs.LogLevel.Log);

        private static void SetLogLevel(Logs.LogLevel level)
        {
            Logs.CurrentLevel = level;
            EditorPrefs.SetInt(PREF_KEY, (int)level);
            UpdateMenuChecks();
            Logs.Log($"[DEBUG] Log level set to: {level}");
        }

        private static void UpdateMenuChecks()
        {
            Menu.SetChecked(MENU_ROOT + "None", Logs.CurrentLevel == Logs.LogLevel.None);
            Menu.SetChecked(MENU_ROOT + "Error", Logs.CurrentLevel == Logs.LogLevel.Error);
            Menu.SetChecked(MENU_ROOT + "Warning", Logs.CurrentLevel == Logs.LogLevel.Warning);
            Menu.SetChecked(MENU_ROOT + "Log", Logs.CurrentLevel == Logs.LogLevel.Log);
        }
    }
}