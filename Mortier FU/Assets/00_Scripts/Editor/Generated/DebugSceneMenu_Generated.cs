#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using MortierFu.Shared;

public static class DebugSceneMenu_Generated
{
    private const string PREF_KEY = "DebugSceneName";

    [MenuItem("DEBUG/Scenes/None", false, 0)]
    private static void Select_None()
    {
        EditorPrefs.DeleteKey(PREF_KEY);
        Logs.Log("[DebugSceneMenu] Cleared debug scene selection.");
    }

    [MenuItem("DEBUG/Scenes/None", true)]
    private static bool Validate_None()
    {
        Menu.SetChecked("DEBUG/Scenes/None", string.IsNullOrEmpty(EditorPrefs.GetString(PREF_KEY, "")));
        return true;
    }

    [MenuItem("DEBUG/Scenes/Show Current", false, 1000)]
    private static void ShowCurrent()
    {
        string cur = EditorPrefs.GetString(PREF_KEY, "");
        if (string.IsNullOrEmpty(cur))
            EditorUtility.DisplayDialog("Debug Scene", "No debug scene selected.", "OK");
        else
            EditorUtility.DisplayDialog("Debug Scene", $"Current debug scene:\n{cur}", "OK");
    }
}
#endif
