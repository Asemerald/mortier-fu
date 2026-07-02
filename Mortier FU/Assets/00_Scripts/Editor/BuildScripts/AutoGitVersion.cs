using System;
using System.Diagnostics;
using System.IO;
using MortierFu.Shared;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MortierFu.Editor
{
    public class AutoGitVersion : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            string branch = GetBranch();
            string commit = GetCommit();

            PlayerSettings.bundleVersion = $"{branch}-{commit}";

            Logs.Log($"[AutoGitVersion] Version set to: {PlayerSettings.bundleVersion}");
        }

        private string GetCommit()
        {
            // 1. GitHub Actions (le plus fiable)
            string githubSha = Environment.GetEnvironmentVariable("GITHUB_SHA");
            if (!string.IsNullOrEmpty(githubSha))
                return githubSha.Substring(0, Math.Min(7, githubSha.Length));

            // 2. fallback git local
            return RunGit("rev-parse --short HEAD");
        }

        private string GetBranch()
        {
            // 1. GitHub Actions
            string githubBranch = Environment.GetEnvironmentVariable("GITHUB_REF_NAME");
            if (!string.IsNullOrEmpty(githubBranch))
                return githubBranch;

            // 2. fallback git local
            return RunGit("rev-parse --abbrev-ref HEAD");
        }

        private string RunGit(string args)
        {
            try
            {
                string gitPath = FindGitExecutable();

                var process = new Process();
                process.StartInfo.FileName = gitPath;
                process.StartInfo.Arguments = args;

                process.StartInfo.WorkingDirectory =
                    Directory.GetParent(Application.dataPath)?.FullName;

                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                string output = process.StandardOutput.ReadLine();
                process.WaitForExit();

                return string.IsNullOrWhiteSpace(output) ? "N/A" : output.Trim();
            }
            catch (Exception e)
            {
                Logs.LogWarning($"[AutoGitVersion] Git failed: {e.Message}");
                return "N/A";
            }
        }

        private string FindGitExecutable()
        {
#if UNITY_EDITOR_WIN
            string[] candidates =
            {
                @"C:\Program Files\Git\cmd\git.exe",
                @"C:\Program Files\Git\bin\git.exe",
                "git"
            };

            foreach (var path in candidates)
            {
                if (path == "git" || File.Exists(path))
                    return path;
            }

            return "git";
#else
            return "git";
#endif
        }
    }
}