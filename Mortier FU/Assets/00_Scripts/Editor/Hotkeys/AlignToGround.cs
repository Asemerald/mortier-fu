using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MortierFu.Editor
{
    public static class AlignToGround {
        [MenuItem("Tools/Align To Ground _END")]
        public static void AlignToGroundEND() {
            var selectedObjects = Selection.gameObjects;
        
            Undo.RecordObjects(
                selectedObjects.Select(go => (Object)go.transform).ToArray(), 
                "Align To Ground"
            );
        
            foreach (var obj in selectedObjects) {
                AlignObjectToGround(obj);
            }
        }
    
        public static void AlignObjectToGround(GameObject obj) {
            if (obj == null) return;

            var ray = new Ray(obj.transform.position, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit)) {
                obj.transform.position = hit.point;
            }
        }
    }   
}