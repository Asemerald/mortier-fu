using UnityEditor;
using UnityEngine;

namespace MortierFu.Editor
{
    [CustomPropertyDrawer(typeof(AugmentStatMod))]
    public class AugmentStatModDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Begin property
            EditorGUI.BeginProperty(position, label, property);

            // Draw label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // Don't indent the children
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Calculate rects for Value and ModType fields
            float width = position.width;
            float valueWidth = width * 0.4f;
            float typeWidth = width * 0.6f;
            Rect valueRect = new Rect(position.x, position.y, valueWidth, position.height);
            Rect typeRect = new Rect(position.x + valueWidth + 4, position.y, typeWidth - 4, position.height);

            // Draw fields
            EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("Value"), GUIContent.none);
            EditorGUI.PropertyField(typeRect, property.FindPropertyRelative("ModType"), GUIContent.none);

            // Reset indent
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
