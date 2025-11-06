using UnityEditor;

namespace MortierFu.Editor
{
    [CustomEditor(typeof(SO_AugmentLibrary), true)]
    public class AugmentLibraryEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var holder = target as SO_AugmentLibrary;
            float totalWeight = 0f;
            
            if (holder && holder.AugmentEntries != null && holder.AugmentEntries.Count > 0)
            {
                foreach (var entry in holder.AugmentEntries)
                {
                    totalWeight += entry.Weight;
                }
            }
            
            EditorGUILayout.LabelField("Total Weight", totalWeight.ToString("F2"));
        }
    }
}