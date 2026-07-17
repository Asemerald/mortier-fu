using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class BuildScript
{
    private static string[] Scenes =>
        EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

    private static void Build(BuildTarget target, string path)
    {
        BuildReport report = BuildPipeline.BuildPlayer(
            Scenes,
            path,
            target,
            BuildOptions.None
        );

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new System.Exception($"Build failed : {report.summary.result}");
        }
    }

    public static void BuildWindows()
    {
        Build(BuildTarget.StandaloneWindows64,
            "Build/Windows/Go Kaboom!.exe");
    }

    public static void BuildLinux()
    {
        Build(BuildTarget.StandaloneLinux64,
            "Build/Linux/Go Kaboom!.x86_64");
    }

    public static void BuildMacOS()
    {
        Build(BuildTarget.StandaloneOSX,
            "Build/macOS/Go Kaboom!.app");
    }
}