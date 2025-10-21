using System.Diagnostics;
using UnityEngine;

namespace MortierFu.Shared
{
    public static class Logs
    {
        public enum LogLevel
        {
            None = 0,
            Error = 1,
            Warning = 2,
            Info = 3
        }

        private static string InfoColor = "white";
        private static string WarningColor = "yellow";
        private static string ErrorColor = "red";

        // Niveau de log actuel (modifiable via le menu DEBUG)
        public static LogLevel CurrentLevel = LogLevel.Info;

        // Log Info
        [Conditional("DEBUG")]
        public static void Log(string message, Object context = null)
        {
            if (CurrentLevel < LogLevel.Info) return;
            if (context)
                UnityEngine.Debug.Log($"<color={InfoColor}>{message}</color>", context);
            else
                UnityEngine.Debug.Log($"<color={InfoColor}>{message}</color>");
        }

        // Log Warning
        [Conditional("DEBUG")]
        public static void LogWarning(string message, Object context = null)
        {
            if (CurrentLevel < LogLevel.Warning) return;
            if (context)
                UnityEngine.Debug.LogWarning($"<color={WarningColor}>{message}</color>", context);
            else
                UnityEngine.Debug.LogWarning($"<color={WarningColor}>{message}</color>");
        }

        // Log Error
        [Conditional("DEBUG")]
        public static void LogError(string message, Object context = null)
        {
            if (CurrentLevel < LogLevel.Error) return;
            if (context)
                UnityEngine.Debug.LogError($"<color={ErrorColor}>{message}</color>", context);
            else
                UnityEngine.Debug.LogError($"<color={ErrorColor}>{message}</color>");
        }
    }
}