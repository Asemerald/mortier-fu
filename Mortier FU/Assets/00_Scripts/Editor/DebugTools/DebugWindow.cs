using Eflatun.SceneReference;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using MortierFu.Shared;

namespace MortierFu.Editor {
    public class DebugWindow : EditorWindow {
        [SerializeField] private SceneReference _overrideMap;
        
        private VisualElement _contentContainer;
        private ToolbarButton _settingsTab, _balancingTab;
        private SerializedObject _serializedObject;
        
        private const string k_skipMenuEnabled = "SkipMenuEnabled";
        private const string k_overrideMapAddress = "OverrideMapAddress";
        
        private static bool SkipMenuEnabled {
            get => EditorPrefs.GetBool(k_skipMenuEnabled);
            set => EditorPrefs.SetBool(k_skipMenuEnabled, value);
        }

        private static string OverrideMapAddress {
            get => EditorPrefs.GetString(k_overrideMapAddress, "");
            set => EditorPrefs.SetString(k_overrideMapAddress, value);
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

            string address = OverrideMapAddress;
            if (!string.IsNullOrEmpty(address)) {
                _overrideMap = SceneReference.FromAddress(address);
                if (_overrideMap == null) {
                    Logs.LogWarning($"[DebugWindow]: The following address was retrieved for the override map {address} but could not find a corresponding scene !");
                }
            }
            
            var overrideMapProp = _serializedObject.FindProperty("_overrideMap");
            var overrideMapField = new PropertyField(overrideMapProp) {
                label = "Override Map",
                tooltip = "This scene will always be used to override the random selection of a map.",
            };
            overrideMapField.Bind(_serializedObject);
            
            overrideMapField.RegisterCallback<SerializedPropertyChangeEvent>(evt => {
                switch (_overrideMap.State) {
                    case SceneReferenceState.Addressable:
                        OverrideMapAddress = _overrideMap.Address;
                        Logs.Log("[DebugWindow]: Changed override map address to : " + OverrideMapAddress);
                        break;
                    case SceneReferenceState.Regular:
                        OverrideMapAddress = "";
                        Logs.LogWarning("[DebugWindow]: Trying to set a non-addressable scene as the override map ! Make sure to make it addressable.");
                        break;
                    case SceneReferenceState.Unsafe:
                        OverrideMapAddress = "";
                        if (_overrideMap.UnsafeReason == SceneReferenceUnsafeReason.Empty) {
                            Logs.Log("[DebugWindow]: Random map selection re-enabled.");
                        }
                        else {
                            Logs.LogWarning("[DebugWindow]: The selected override map is probably not in the build settings.");
                        }
                        break;
                }
            });
            
            _contentContainer.AddHeader("LD / LA");
            _contentContainer.Add(overrideMapField);
        }

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
