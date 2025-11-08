using NaughtyAttributes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.AddressableAssets;
using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_AugmentSelectionSettings", menuName = "Mortier Fu/Settings/AugmentSelection")]
    public class SO_AugmentSelectionSettings : SO_SystemSettings
    {
        [Header("Timing")]
        [Tooltip("Delay before starting the augment showcase (in seconds).")]
        public float ShowcaseStartDelay = 2f;

        [Tooltip("Time it takes for each card to scale up from zero when the showcase starts.")]
        public float CardPopInDuration = 1.3f;

        [Tooltip("Pause between the showcase ending and moving the cards to their level positions.")]
        public float MoveCardsToTargetDelay = 2f;

        [Tooltip("Delay before restoring player input after all animations have finished.")]
        public float PlayerInputReenableDelay = 2f;

        [Header("Card Animation Ranges")]
        [Tooltip("Randomized duration range for the movement and scaling animation when each card moves to its target position.")]
        public MinMaxRange CardMoveDurationRange = new(0.4f, 0.8f);

        [Tooltip("Randomized delay range between each card’s movement animation (stagger).")]
        public MinMaxRange CardMoveStaggerRange = new(0.12f, 0.4f);

        [Header("Visuals")]
        [Tooltip("Final scale applied to each augment card during showcase and placement.")]
        public float DisplayedCardScale = 4f;

        [Tooltip("Horizontal distance between augment cards during showcase.")]
        public float CardSpacing = 2.2f;
        
        [Header("References")]
        public AssetReferenceGameObject AugmentPickupPrefab;

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
        
        [System.Serializable]
        public struct MinMaxRange
        {
            [BoxGroup("Range Values")]
            public float Min;
            [BoxGroup("Range Values")]
            public float Max;

            public MinMaxRange(float min, float max)
            {
                Min = min;
                Max = max;
            }
            
            public float GetRandomValue()
            {
                return Random.Range(Min, Max);
            }
        }
    }
}

