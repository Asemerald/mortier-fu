#if UNITY_EDITOR
using System.IO;
using MortierFu.Shared;
using UnityEditor;
using UnityEngine;

public static class SaveDebugMenu
{
    [MenuItem("Debug/Save/Delete Tutorial Save")]
    public static void DeleteTutorialSave()
    {
        string path = Path.Combine(Application.persistentDataPath, "tutorial.json");

        if (File.Exists(path))
        {
            File.Delete(path);
            Logs.Log($"[SaveDebugMenu] Deleted: {path}");
        }
        else
        {
            Logs.Log("[SaveDebugMenu] No tutorial save found.");
        }
    }

    [MenuItem("Debug/Save/Open Save Folder")]
    public static void OpenSaveFolder()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}
#endif