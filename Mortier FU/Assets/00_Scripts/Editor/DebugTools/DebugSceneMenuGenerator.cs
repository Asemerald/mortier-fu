#if UNITY_EDITOR
using System.IO;
using System.Text;
using MortierFu.Shared;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
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

        // c'est ici que la magie opère 
        // jrigole ta grand mere c'est un enfer 
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

        // --- None ---
        sb.AppendLine("    [MenuItem(\"DEBUG/Scenes/None\", false, 0)]");
        sb.AppendLine("    private static void Select_None()");
        sb.AppendLine("    {");
        sb.AppendLine("        EditorPrefs.DeleteKey(PREF_KEY);");
        sb.AppendLine("        Logs.Log(\"[DebugSceneMenu] Cleared debug scene selection.\");");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    [MenuItem(\"DEBUG/Scenes/None\", true)]");
        sb.AppendLine("    private static bool Validate_None()");
        sb.AppendLine("    {");
        sb.AppendLine("        Menu.SetChecked(\"DEBUG/Scenes/None\", string.IsNullOrEmpty(EditorPrefs.GetString(PREF_KEY, \"\")));");
        sb.AppendLine("        return true;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // --- Toutes les scènes du Build Settings ---
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < sceneCount; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = Path.GetFileNameWithoutExtension(path);
            string safeMethodName = MakeSafeName(name);

            // Select method
            sb.AppendLine($"    [MenuItem(\"DEBUG/Scenes/{EscapeForMenu(name)}\", false, {i + 1})]");
            sb.AppendLine($"    private static void Select_{safeMethodName}()");
            sb.AppendLine("    {");
            sb.AppendLine($"        EditorPrefs.SetString(PREF_KEY, \"{EscapeForCSharpLiteral(name)}\");");
            sb.AppendLine($"        Logs.Log(\"[DebugSceneMenu] Debug scene set to: {EscapeForCSharpLiteral(name)}\");");
            sb.AppendLine("    }");
            sb.AppendLine();

            // Validate method (pour cocher la scène)
            sb.AppendLine($"    [MenuItem(\"DEBUG/Scenes/{EscapeForMenu(name)}\", true)]");
            sb.AppendLine($"    private static bool Validate_{safeMethodName}()");
            sb.AppendLine("    {");
            sb.AppendLine($"        Menu.SetChecked(\"DEBUG/Scenes/{EscapeForMenu(name)}\", EditorPrefs.GetString(PREF_KEY, \"\") == \"{EscapeForCSharpLiteral(name)}\");");
            sb.AppendLine("        return true;");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        // --- Utilitaire pour afficher la scène actuelle ---
        sb.AppendLine("    [MenuItem(\"DEBUG/Scenes/Show Current\", false, 1000)]");
        sb.AppendLine("    private static void ShowCurrent()");
        sb.AppendLine("    {");
        sb.AppendLine("        string cur = EditorPrefs.GetString(PREF_KEY, \"\");");
        sb.AppendLine("        if (string.IsNullOrEmpty(cur))");
        sb.AppendLine("            EditorUtility.DisplayDialog(\"Debug Scene\", \"No debug scene selected.\", \"OK\");");
        sb.AppendLine("        else");
        sb.AppendLine("            EditorUtility.DisplayDialog(\"Debug Scene\", $\"Current debug scene:\\n{cur}\", \"OK\");");
        sb.AppendLine("    }");

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
