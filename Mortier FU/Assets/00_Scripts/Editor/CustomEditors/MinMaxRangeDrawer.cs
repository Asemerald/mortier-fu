using MortierFu.Shared;
using UnityEditor;
using UnityEngine;

namespace MortierFu.Editor
{
    [CustomPropertyDrawer(typeof(MinMaxRange))]
    public class MinMaxRangeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            Rect minRect = new Rect(labelRect.xMax, position.y, (position.width - EditorGUIUtility.labelWidth) / 2f - 2f, position.height);
            Rect maxRect = new Rect(minRect.xMax + 4f, position.y, (position.width - EditorGUIUtility.labelWidth) / 2f - 2f, position.height);

            SerializedProperty minProp = property.FindPropertyRelative("Min");
            SerializedProperty maxProp = property.FindPropertyRelative("Max");

            EditorGUI.LabelField(labelRect, label);
            EditorGUI.PropertyField(minRect, minProp, GUIContent.none);
            EditorGUI.PropertyField(maxRect, maxProp, GUIContent.none);

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}