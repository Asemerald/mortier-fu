using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MortierFu.Editor
{
    public class SceneOpenerEditor : EditorWindow
    {
        [MenuItem("Tools/Scene Opener")]
        public static void ShowWindow()
        {
            GetWindow<SceneOpenerEditor>("Scene Opener");
        }

        private Vector2 _scrollPosition;
        private string _targetFolderPath = "Assets/Scenes";
        private int _columns = 4;
        private float _buttonHeight = 30f;
        private float _padding = 10f;
        

        private void OnGUI()
        {
            GUILayout.Label("Scene Opener", EditorStyles.boldLabel);
            GUILayout.Label($"Scenes in Folder: {_targetFolderPath}", EditorStyles.label);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Width(position.width),
                GUILayout.Height(position.height - 40));

            string[] sceneGUIDs = AssetDatabase.FindAssets("t:Scene", new[] { _targetFolderPath });
        
            if (sceneGUIDs.Length == 0)
            {
                EditorGUILayout.LabelField("No scenes found in the specified folder.");
            }
            else
            {
                var buildScenes = EditorBuildSettings.scenes.Select(s => s.path).ToList();
            
                var sceneBuildIndexMap = EditorBuildSettings.scenes
                    .Select((scene, index) => new { scene.path, index })
                    .ToDictionary(x => x.path, x => x.index);
            
                var buildScenesInFolder = sceneGUIDs.Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .Where(path => buildScenes.Contains(path))
                    .OrderBy(path => sceneBuildIndexMap.ContainsKey(path) ? sceneBuildIndexMap[path] : int.MaxValue)
                    .ToList();
            
                var nonBuildScenesInFolder = sceneGUIDs.Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .Where(path => !buildScenes.Contains(path))
                    .OrderBy(path => System.IO.Path.GetFileNameWithoutExtension(path))
                    .ToList();

                GUILayout.Label("Scenes in Build Settings:", EditorStyles.boldLabel);
                DisplayScenesGrid(buildScenesInFolder, true);

                GUILayout.Space(10);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                GUILayout.Space(10);

                GUILayout.Label("Scenes not in Build Settings:", EditorStyles.boldLabel);
                DisplayScenesGrid(nonBuildScenesInFolder, true);
            }

            GUILayout.EndScrollView();
        }


        private void DisplayScenesGrid(System.Collections.Generic.List<string> scenePaths, bool isInBuildSettings)
        {
            var totalScenes = scenePaths.Count;
            var rows = Mathf.CeilToInt((float)totalScenes / _columns);
            var buttonWidth = (position.width - _padding * (_columns - 1)) / _columns;

            var marketAmount = scenePaths.Count(s => System.IO.Path.GetFileNameWithoutExtension(s).StartsWith("Market"));
            var marketID = 0;

            for (var row = 0; row < rows; row++)
            {
                GUILayout.BeginHorizontal();
                for (var col = 0; col < _columns; col++)
                {
                    var index = row * _columns + col;
                    if (index < totalScenes)
                    {
                        var scenePath = scenePaths[index];
                        var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                        var oldColor = GUI.backgroundColor;

                        var buttonStyle = new GUIStyle(GUI.skin.button);
                        if (sceneName.StartsWith("Market"))
                        {
                            var match = Regex.Match(sceneName, @"Market(\d+)");
                            if (match.Success)
                            {
                                var darkness = 0.6f;
                                GUI.backgroundColor = Color.Lerp(
                                    new Color(darkness, 1.0f, darkness),
                                    new Color(1.0f, darkness, darkness),
                                    marketID / (float)marketAmount);
                            }

                            marketID++;
                        }

                        GUILayout.BeginVertical();

                        GUI.enabled = isInBuildSettings;

                        if (GUILayout.Button(sceneName, buttonStyle, GUILayout.Width(buttonWidth - 20),
                                GUILayout.Height(_buttonHeight)))
                            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                                EditorSceneManager.OpenScene(scenePath);

                        GUI.enabled = true;

                        GUILayout.EndVertical();

                        GUI.backgroundColor = oldColor;
                    }
                    else
                    {
                        GUILayout.FlexibleSpace();
                    }
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(_padding);
            }
        }
    }
}