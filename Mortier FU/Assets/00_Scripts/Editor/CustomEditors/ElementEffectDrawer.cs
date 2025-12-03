using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System;

namespace MortierFu
{
    [CustomPropertyDrawer(typeof(IEffect<>), true)]
    public class ElementEffectDrawer : PropertyDrawer
    {
        private static Dictionary<string, Type> _typeMap;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (_typeMap == null) BuildTypeMap();

            var typeRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var contentRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width,
                position.height - EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginProperty(position, label, property);
            var typeName = property.managedReferenceFullTypename;
            var displayName = GetShortTypeName(typeName);

            if (EditorGUI.DropdownButton(typeRect, new GUIContent(displayName ?? "Select State Type"),
                    FocusType.Keyboard))
            {
                var menu = new GenericMenu();
                if (_typeMap == null || _typeMap.Count == 0)
                {
                    menu.AddDisabledItem(new GUIContent("No State effects available"));
                    menu.ShowAsContext();
                    return;
                }

                foreach (var kvp in _typeMap)
                {
                    var name = kvp.Key;
                    var type = kvp.Value;
                    menu.AddItem(new GUIContent(name), type.FullName == typeName, () =>
                    {
                        property.managedReferenceValue = Activator.CreateInstance(type);
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }

                menu.ShowAsContext();
            }

            if (property.managedReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                EditorGUI.PropertyField(contentRect, property, GUIContent.none, true);
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true) + EditorGUIUtility.singleLineHeight;
        }

        static void BuildTypeMap()
        {
            var baseGenericType = typeof(IEffect<>);
            _typeMap = new Dictionary<string, Type>();

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch { continue; }

                foreach (var t in types)
                {
                    if (t.IsAbstract) continue;
                    if (t.IsInterface) continue;

                    var interfaces = t.GetInterfaces();

                    foreach (var iface in interfaces)
                    {
                        if (iface.IsGenericType &&
                            iface.GetGenericTypeDefinition() == baseGenericType)
                        {
                            var niceName = ObjectNames.NicifyVariableName(t.Name);
                            _typeMap[niceName] = t;
                            break;
                        }
                    }
                }
            }
        }


        static string GetShortTypeName(string fullTypeName)
        {
            if (string.IsNullOrEmpty(fullTypeName)) return null;
            var parts = fullTypeName.Split(' ');
            return parts.Length > 1 ? parts[1].Split('.').Last() : fullTypeName;
        }
    }
}