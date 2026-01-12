using System.Linq;
using UnityEditor;
public static class BuildScript
{
    // Only call by Github Actions CI
    public static void BuildWindows()
    {
        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        BuildPipeline.BuildPlayer(
            scenes,
            "Build/Windows/Mortar Game.exe",
            BuildTarget.StandaloneWindows64,
            BuildOptions.None
        );
    }
}
