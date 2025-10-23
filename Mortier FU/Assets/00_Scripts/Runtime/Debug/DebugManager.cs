using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using MortierFu.Shared;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DebugManager : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugMode = true;
    [SerializeField] private bool showFPS = false;
    [SerializeField] private bool showConsoleOnScreen = false;
    [SerializeField] private KeyCode toggleConsoleKey = KeyCode.F1;
    [SerializeField] private KeyCode toggleFPSKey = KeyCode.F2;
    [SerializeField] private KeyCode screenshotKey = KeyCode.F12;
    
    [Header("Performance")]
    [SerializeField] private bool showMemoryUsage = false;
    [SerializeField] private float performanceUpdateInterval = 0.5f;
    [SerializeField] private KeyCode toggleMemoryKey = KeyCode.F3;
    
    // FPS tracking
    private float deltaTime = 0.0f;
    private float fps = 0.0f;
    private float lastPerformanceUpdate = 0.0f;
    
    // Console on screen
    private List<string> consoleMessages = new List<string>();
    private const int maxConsoleMessages = 20;
    private Vector2 consoleScrollPosition;
    private bool consoleVisible = false;
    
    // GUI Style
    private GUIStyle consoleStyle;
    private GUIStyle fpsStyle;
    private bool stylesInitialized = false;
    
    // Singleton pattern
    private static DebugManager instance;
    public static DebugManager Instance => instance;

    private void Awake()
    {
        // Singleton setup
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Subscribe to log messages
        Application.logMessageReceived += HandleLog;
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void Update()
    {
        if (!enableDebugMode) return;
        
        HandleDebugInputs();
        UpdatePerformanceMetrics();
    }

    private void HandleDebugInputs()
    {
        // Restart game: Ctrl+R
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
        
        // Toggle console: F1 (or custom key)
        if (Input.GetKeyDown(toggleConsoleKey))
        {
            consoleVisible = !consoleVisible;
        }
        
        // Toggle FPS: F2 (or custom key)
        if (Input.GetKeyDown(toggleFPSKey))
        {
            showFPS = !showFPS;
        }
        
        // Screenshot: F12 (or custom key)
        if (Input.GetKeyDown(screenshotKey))
        {
            TakeScreenshot();
        }
        
        // Time scale controls
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                IncreaseTimeScale();
            }
            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                DecreaseTimeScale();
            }
            if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
            {
                ResetTimeScale();
            }
        }
        
        // Pause toggle: Ctrl+P
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.P))
        {
            TogglePause();
        }
        
        // Toggle memory usage display: F3 (or custom key)
        if (Input.GetKeyDown(toggleMemoryKey))
        {
            showMemoryUsage = !showMemoryUsage;
        }
        
        // Log system info: Ctrl+I
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.I))
        {            
            LogSystemInfo();
        }
    }

    private void UpdatePerformanceMetrics()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        
        if (Time.unscaledTime - lastPerformanceUpdate >= performanceUpdateInterval)
        {
            fps = 1.0f / deltaTime;
            lastPerformanceUpdate = Time.unscaledTime;
        }
    }

    private void OnGUI()
    {
        if (!enableDebugMode) return;
        
        InitializeStyles();
        
        if (showFPS)
        {
            DrawFPS();
        }
        
        if (showMemoryUsage)
        {
            DrawMemoryUsage();
        }
        
        if (consoleVisible || showConsoleOnScreen)
        {
            DrawConsole();
        }
    }

    private void InitializeStyles()
    {
        if (stylesInitialized) return;
        
        consoleStyle = new GUIStyle(GUI.skin.box);
        consoleStyle.fontSize = 12;
        consoleStyle.alignment = TextAnchor.UpperLeft;
        consoleStyle.wordWrap = false;
        consoleStyle.normal.textColor = Color.white;
        consoleStyle.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.8f));
        
        fpsStyle = new GUIStyle(GUI.skin.label);
        fpsStyle.fontSize = 18;
        fpsStyle.fontStyle = FontStyle.Bold;
        fpsStyle.normal.textColor = Color.white;
        fpsStyle.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.5f));
        
        stylesInitialized = true;
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
        
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void DrawFPS()
    {
        float x = 10;
        float y = 10;
        float w = 150;
        float h = 40;
        
        Color fpsColor = fps >= 60 ? Color.green : fps >= 30 ? Color.yellow : Color.red;
        fpsStyle.normal.textColor = fpsColor;
        
        GUI.Label(new Rect(x, y, w, h), $"FPS: {fps:F1}", fpsStyle);
    }

    private void DrawMemoryUsage()
    {
        float x = 10;
        float y = showFPS ? 60 : 10;
        float w = 250;
        float h = 40;
        
        long totalMemory = GC.GetTotalMemory(false) / 1048576; // Convert to MB
        GUI.Label(new Rect(x, y, w, h), $"Memory: {totalMemory} MB", fpsStyle);
    }

    private void DrawConsole()
    {
        float w = Screen.width * 0.8f;
        float h = Screen.height * 0.3f;
        float x = (Screen.width - w) / 2;
        float y = Screen.height - h - 10;
        
        GUILayout.BeginArea(new Rect(x, y, w, h), consoleStyle);
        consoleScrollPosition = GUILayout.BeginScrollView(consoleScrollPosition);
        
        foreach (string msg in consoleMessages)
        {
            GUILayout.Label(msg, consoleStyle);
        }
        
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        string prefix = type switch
        {
            LogType.Error => "[ERROR] ",
            LogType.Warning => "[WARN] ",
            LogType.Exception => "[EXCEPTION] ",
            _ => ""
        };
        
        consoleMessages.Add($"{prefix}{logString}");
        
        if (consoleMessages.Count > maxConsoleMessages)
        {
            consoleMessages.RemoveAt(0);
        }
    }

    // === PUBLIC DEBUG METHODS ===
    
    public static void RestartGame()
    {
#if UNITY_EDITOR
        Logs.Log("Restarting editor playmode...", null);
        EditorApplication.isPlaying = false;
        // Use coroutine or delayed call to restart
        EditorApplication.update += RestartPlayMode;
        return;
#endif
        string dataPath = Application.dataPath;
        string parentPath = Directory.GetParent(dataPath)?.FullName;
        if (parentPath != null)
        {
            string exePath = Path.Combine(parentPath, "Mortier FU.exe");
            if (File.Exists(exePath))
            {
                Logs.Log("Restarting game...", null);
                Process.Start(exePath);
                Application.Quit();
            }
            else
            {
                Logs.LogError("Could not find executable to restart the game.", null);
            }
        }
    }

#if UNITY_EDITOR
    private static void RestartPlayMode()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorApplication.update -= RestartPlayMode;
            EditorApplication.isPlaying = true;
        }
    }
#endif

    public static void TakeScreenshot()
    {
        string folderPath = Path.Combine(Application.dataPath, "..", "Screenshots");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        
        string filename = $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
        string fullPath = Path.Combine(folderPath, filename);
        
        ScreenCapture.CaptureScreenshot(fullPath);
        Logs.Log($"Screenshot saved: {fullPath}", null);
    }

    public static void TogglePause()
    {
        Time.timeScale = Time.timeScale > 0 ? 0 : 1;
        Logs.Log($"Game {(Time.timeScale > 0 ? "resumed" : "paused")}", null);
    }

    public static void IncreaseTimeScale()
    {
        Time.timeScale = Mathf.Min(Time.timeScale + 0.5f, 5f);
        Logs.Log($"Time scale: {Time.timeScale:F1}x", null);
    }

    public static void DecreaseTimeScale()
    {
        Time.timeScale = Mathf.Max(Time.timeScale - 0.5f, 0.1f);
        Logs.Log($"Time scale: {Time.timeScale:F1}x", null);
    }

    public static void ResetTimeScale()
    {
        Time.timeScale = 1f;
        Logs.Log("Time scale reset to 1x", null);
    }

    public static void LogSystemInfo()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== SYSTEM INFO ===");
        sb.AppendLine($"OS: {SystemInfo.operatingSystem}");
        sb.AppendLine($"CPU: {SystemInfo.processorType} ({SystemInfo.processorCount} cores)");
        sb.AppendLine($"RAM: {SystemInfo.systemMemorySize} MB");
        sb.AppendLine($"GPU: {SystemInfo.graphicsDeviceName}");
        sb.AppendLine($"VRAM: {SystemInfo.graphicsMemorySize} MB");
        sb.AppendLine($"Resolution: {Screen.width}x{Screen.height} @ {Screen.currentResolution.refreshRateRatio}Hz");
        sb.AppendLine($"Unity Version: {Application.unityVersion}");
        
        Logs.Log(sb.ToString(), null);
    }

    // === METHODS FOR GAME-SPECIFIC DEBUG ===
}