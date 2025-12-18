using System.Diagnostics;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MortierFu.Editor
{
    public class AutoGitVersion : IPreprocessBuildWithReport
    {
        // Priorité d'exécution 
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            string commit = GetGitCommit();
            string branch = GetGitBranch();

            // [branch]-[commit]
            PlayerSettings.bundleVersion = $"{branch}-{commit}";
            UnityEngine.Debug.Log($"[AutoGitVersion] Version set to: {PlayerSettings.bundleVersion}");
        }

        private string GetGitCommit()
        {
            return RunGitCommand("rev-parse --short HEAD");
        }

        private string GetGitBranch()
        {
            return RunGitCommand("rev-parse --abbrev-ref HEAD");
        }

        private string RunGitCommand(string args)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "git";
                process.StartInfo.Arguments = args;
                process.StartInfo.WorkingDirectory = Application.dataPath;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                string output = process.StandardOutput.ReadLine();
                process.WaitForExit();
                return string.IsNullOrEmpty(output) ? "N/A" : output.Trim();
            }
            catch
            {
                return "N/A";
            }
        }
    }
}