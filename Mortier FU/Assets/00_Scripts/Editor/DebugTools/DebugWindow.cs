using System;
using Eflatun.SceneReference;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using MortierFu.Shared;
using UnityEngine.Serialization;

namespace MortierFu.Editor {
    public class DebugWindow : EditorWindow {
        [SerializeField] private SceneReference _arenaOverrideMap;
        [SerializeField] private SceneReference _raceOverrideMap;
        
        private VisualElement _contentContainer;
        private ToolbarButton _settingsTab, _balancingTab;
        private SerializedObject _serializedObject;
        
        private const string k_skipMenuEnabled = "SkipMenuEnabled";
        private const string k_overrideArenaMapAddress = "OverrideArenaMapAddress";
        private const string k_overrideRaceMapAddress = "OverrideRaceMapAddress";
        
        private static bool SkipMenuEnabled {
            get => EditorPrefs.GetBool(k_skipMenuEnabled);
            set => EditorPrefs.SetBool(k_skipMenuEnabled, value);
        }
        
        private void CreateGUI() {
            //  This will be used to retrieve serialized properties
            _serializedObject = new SerializedObject(this);
            _serializedObject.Update();
            
            VisualElement root = rootVisualElement;
            
            // Create toolbar
            var toolbar = new Toolbar() {
                style = {
                    height = 30,
                }
            };

            _settingsTab = new ToolbarButton(ShowSettings) {
                text = "Settings",
                style = {
                    paddingTop = 5f,
                    paddingBottom = 5f,
                    paddingLeft = 15f,
                    paddingRight = 15f
                }
            };
            _balancingTab = new ToolbarButton(ShowBalancing) {
                text = "Balancing",
                style = {
                    paddingTop = 5f,
                    paddingBottom = 5f,
                    paddingLeft = 15f,
                    paddingRight = 15f
                }
            };
            
            toolbar.Add(_settingsTab);
            toolbar.Add(_balancingTab);
            
            root.Add(toolbar);

            _contentContainer = new VisualElement();
            _contentContainer.style.flexGrow = 1f;
            _contentContainer.SetMargin(10f);
            root.Add(_contentContainer);

            ShowSettings();
        }

        private void ShowSettings() {
            _balancingTab.style.unityFontStyleAndWeight = FontStyle.Normal;
            _settingsTab.style.unityFontStyleAndWeight = FontStyle.Bold;
            _contentContainer.Clear();

            var skipMenuToggle = new Toggle("Skip Menu") {
                tooltip = "If toggled, the system will detect connected devices, make them join and skip the main menu to launch the game as fast as possible.",
                value = SkipMenuEnabled,
            };
            skipMenuToggle.RegisterValueChangedCallback(evt => SkipMenuEnabled = evt.newValue);
            
            _contentContainer.AddHeader("GLOBAL");
            _contentContainer.Add(skipMenuToggle);
            
            string arenaMapAddress = ReadOverrideMapAddress(k_overrideArenaMapAddress);
            if (!string.IsNullOrEmpty(arenaMapAddress)) {
                _arenaOverrideMap = SceneReference.FromAddress(arenaMapAddress);
                if (_arenaOverrideMap == null) {
                    Logs.LogWarning($"[DebugWindow]: The following address was retrieved for the override map {arenaMapAddress} but could not find a corresponding scene !");
                }
            }
            
            var overrideArenaMapField = MakeOverrideMapProperty("_arenaOverrideMap");
            overrideArenaMapField.RegisterCallback<SerializedPropertyChangeEvent, MapOverrideData>(OnOverrideMapChanged, new MapOverrideData
            {
                Key = k_overrideArenaMapAddress,
                SceneRef = _arenaOverrideMap
            });
            
            string raceMapAddress = ReadOverrideMapAddress(k_overrideRaceMapAddress);
            if (!string.IsNullOrEmpty(raceMapAddress)) {
                _raceOverrideMap = SceneReference.FromAddress(raceMapAddress);
                if (_arenaOverrideMap == null) {
                    Logs.LogWarning($"[DebugWindow]: The following address was retrieved for the override map {raceMapAddress} but could not find a corresponding scene !");
                }
            }   

            var overrideRaceMapField = MakeOverrideMapProperty("_raceOverrideMap");
            overrideRaceMapField.RegisterCallback<SerializedPropertyChangeEvent, MapOverrideData>(OnOverrideMapChanged, new MapOverrideData
            {
                Key = k_overrideRaceMapAddress,
                SceneRef = _raceOverrideMap
            });

            _contentContainer.AddHeader("LD / LA");
            _contentContainer.Add(overrideArenaMapField);
            _contentContainer.Add(overrideRaceMapField);
        }
        
        struct MapOverrideData
        {
            public string Key;
            public SceneReference SceneRef;
        }
        
        private PropertyField MakeOverrideMapProperty(string property)
        {
            var overrideMapProp = _serializedObject.FindProperty(property);
            var overrideMapField = new PropertyField(overrideMapProp) {
                tooltip = "This scene will always be used to override the random selection of the map."
            };
            overrideMapField.Bind(_serializedObject);
            
            return overrideMapField;
        }
        
        private void OnOverrideMapChanged(SerializedPropertyChangeEvent evt, MapOverrideData data)
        {
            switch (data.SceneRef.State)
            {
                case SceneReferenceState.Addressable:
                    var address = data.SceneRef.Address;
                    WriteOverrideMapAddress(data.Key, address);
                    Logs.Log("[DebugWindow]: Changed override map address to : " + address);
                    break;
                case SceneReferenceState.Regular:
                    WriteOverrideMapAddress(data.Key, "");
                    Logs.LogWarning("[DebugWindow]: Trying to set a non-addressable scene as the override map ! Make sure to make it addressable.");
                    break;
                case SceneReferenceState.Unsafe:
                    WriteOverrideMapAddress(data.Key, "");
                    if (data.SceneRef.UnsafeReason == SceneReferenceUnsafeReason.Empty)
                    {
                        Logs.Log("[DebugWindow]: Random map selection re-enabled.");
                    }
                    else
                    {
                        Logs.LogWarning("[DebugWindow]: The selected override map is probably not in the build settings.");
                    }
                    break;
            }
        }

        private string ReadOverrideMapAddress(string key, string defaultValue = "") => EditorPrefs.GetString(key, defaultValue);
        private void WriteOverrideMapAddress(string key, string value) => EditorPrefs.SetString(key, value);
        
        private void ShowBalancing() {
            _settingsTab.style.unityFontStyleAndWeight = FontStyle.Normal;
            _balancingTab.style.unityFontStyleAndWeight = FontStyle.Bold;
            _contentContainer.Clear();
            
            _contentContainer.AddHeader("Balancing");
            _contentContainer.Add(new Label("Coming soon..."));
        }
        
        [MenuItem("Tools/Debug Window %#M")]
        public static void ShowWindow() {
            DebugWindow wnd = GetWindow<DebugWindow>();
            wnd.titleContent = new GUIContent("Debug Window for Mortier Fuuuuuu");
        }
    }
}
