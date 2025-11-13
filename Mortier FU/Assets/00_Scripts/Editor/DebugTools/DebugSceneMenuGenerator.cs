#if UNITY_EDITOR
using System.IO;
using System.Text;
using MortierFu.Shared;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DebugSceneMenuGenerator
{
    private const string generatedFolder = "Assets/00_Scripts/Editor/Generated";
    private const string generatedFile = generatedFolder + "/DebugSceneMenu_Generated.cs";
    private const string PREF_KEY = "DebugSceneName";

    static DebugSceneMenuGenerator()
    {
        GenerateMenuFile();
    }

    [MenuItem("DEBUG/Scenes/Regenerate Menu")]
    public static void RegenerateMenu()
    {
        GenerateMenuFile();
        AssetDatabase.Refresh();
        Logs.Log("[DebugSceneMenuGenerator] Menu regenerated.");
    }

    private static void GenerateMenuFile()
    {
        if (!Directory.Exists(generatedFolder))
            Directory.CreateDirectory(generatedFolder);

        var sb = new StringBuilder();
        sb.AppendLine("#if UNITY_EDITOR");
        sb.AppendLine("using UnityEditor;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using MortierFu.Shared;");
        sb.AppendLine();
        sb.AppendLine("public static class DebugSceneMenu_Generated");
        sb.AppendLine("{");
        sb.AppendLine($"    private const string PREF_KEY = \"{PREF_KEY}\";");
        sb.AppendLine();

        // --- Toutes les scènes du Build Settings ---
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < sceneCount; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = Path.GetFileNameWithoutExtension(path);
            string safeMethodName = MakeSafeName(name);
            string escapedName = EscapeForCSharpLiteral(name);
            string escapedMenu = EscapeForMenu(name);

            sb.AppendLine($"    [MenuItem(\"DEBUG/Scenes/{escapedMenu}\", false, {i + 1})]");
            sb.AppendLine($"    private static void Select_{safeMethodName}()");
            sb.AppendLine("    {");
            sb.AppendLine($"        string current = EditorPrefs.GetString(PREF_KEY, \"\");");
            sb.AppendLine($"        if (current == \"{escapedName}\")");
            sb.AppendLine("        {");
            sb.AppendLine("            // Uncheck: clear pref");
            sb.AppendLine("            EditorPrefs.DeleteKey(PREF_KEY);");
            sb.AppendLine("            Logs.Log($\"[DebugSceneMenu] Unselected debug scene: {current}\");");
            sb.AppendLine("        }");
            sb.AppendLine("        else");
            sb.AppendLine("        {");
            sb.AppendLine($"            EditorPrefs.SetString(PREF_KEY, \"{escapedName}\");");
            sb.AppendLine($"            Logs.Log(\"[DebugSceneMenu] Debug scene set to: {escapedName}\");");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();

            // Validate pour cocher la scène sélectionnée
            sb.AppendLine($"    [MenuItem(\"DEBUG/Scenes/{escapedMenu}\", true)]");
            sb.AppendLine($"    private static bool Validate_{safeMethodName}()");
            sb.AppendLine("    {");
            sb.AppendLine($"        bool isActive = EditorPrefs.GetString(PREF_KEY, \"\") == \"{escapedName}\";");
            sb.AppendLine($"        Menu.SetChecked(\"DEBUG/Scenes/{escapedMenu}\", isActive);");
            sb.AppendLine("        return true;");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("}");
        sb.AppendLine("#endif");

        File.WriteAllText(generatedFile, sb.ToString(), Encoding.UTF8);
    }

    private static string MakeSafeName(string s)
    {
        var sb = new StringBuilder();
        foreach (char c in s)
        {
            if (char.IsLetterOrDigit(c) || c == '_') sb.Append(c);
            else sb.Append('_');
        }
        if (sb.Length == 0) return "scene";
        if (char.IsDigit(sb[0])) sb.Insert(0, '_');
        return sb.ToString();
    }

    private static string EscapeForMenu(string s)
    {
        return s.Replace("/", " / ");
    }

    private static string EscapeForCSharpLiteral(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
#endif
