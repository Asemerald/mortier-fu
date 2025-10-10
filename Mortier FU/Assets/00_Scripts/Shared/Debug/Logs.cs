using UnityEngine;

namespace MortierFu.Shared
{
    public static class Logs
    {
        // Active ou désactive tous les logs
        public static bool EnableLogs = true;

        // Couleurs pour Unity Console
        private static string InfoColor = "white";
        private static string WarningColor = "yellow";
        private static string ErrorColor = "red";

        // Info log
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Log(string message)
        {
            if (!EnableLogs) return;
            UnityEngine.Debug.Log($"<color={InfoColor}>[INFO] {message}</color>");
        }

        // Warning log
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogWarning(string message)
        {
            if (!EnableLogs) return;
            UnityEngine.Debug.LogWarning($"<color={WarningColor}>[WARN] {message}</color>");
        }

        // Error log
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogError(string message)
        {
            if (!EnableLogs) return;
            UnityEngine.Debug.LogError($"<color={ErrorColor}>[ERROR] {message}</color>");
        }

        // Optionnel : log avec contexte Unity (GameObject)
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Log(string message, Object context)
        {
            if (!EnableLogs) return;
            UnityEngine.Debug.Log($"<color={InfoColor}>[INFO] {message}</color>", context);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogWarning(string message, Object context)
        {
            if (!EnableLogs) return;
            UnityEngine.Debug.LogWarning($"<color={WarningColor}>[WARN] {message}</color>", context);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogError(string message, Object context)
        {
            if (!EnableLogs) return;
            UnityEngine.Debug.LogError($"<color={ErrorColor}>[ERROR] {message}</color>", context);
        }
    }
}