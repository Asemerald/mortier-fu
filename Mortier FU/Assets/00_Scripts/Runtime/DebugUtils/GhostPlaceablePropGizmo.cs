using UnityEngine;

namespace MortierFu
{
    [ExecuteAlways]
    public sealed class GhostPlaceablePropGizmo : MonoBehaviour
    {
        private enum GizmoOriginMode
        {
            PropPivot,
            PlacementOrigin
        }

        [Header("Prop")]
        [SerializeField] private SO_GhostPlaceableProp _prop;

        [Header("Mode")]
        [SerializeField] private GizmoOriginMode _originMode = GizmoOriginMode.PropPivot;

        [Header("Preview")]
        [SerializeField] private bool _drawSpawnPoint = true;
        [SerializeField] private bool _drawPlacementOrigin = true;
        [SerializeField] private bool _drawValidationBox = true;
        [SerializeField] private bool _drawOnlyWhenSelected = true;

        [Header("Colors")]
        [SerializeField] private Color _spawnColor = Color.cyan;
        [SerializeField] private Color _originColor = Color.white;
        [SerializeField] private Color _validationColor = Color.yellow;

        private void OnDrawGizmos()
        {
            if (_drawOnlyWhenSelected)
                return;

            DrawGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            if (!_drawOnlyWhenSelected)
                return;

            DrawGizmos();
        }

        private void DrawGizmos()
        {
            if (!_prop)
                return;

            Quaternion spawnRotation = transform.rotation * _prop.RotationOffset;

            Vector3 spawnPosition;
            Vector3 placementOrigin;

            if (_originMode == GizmoOriginMode.PropPivot)
            {
                spawnPosition = transform.position;
                placementOrigin = spawnPosition - spawnRotation * _prop.SpawnOffset;
            }
            else
            {
                placementOrigin = transform.position;
                spawnPosition = placementOrigin + spawnRotation * _prop.SpawnOffset;
            }

            if (_drawPlacementOrigin)
                DrawPlacementOrigin(placementOrigin);

            if (_drawSpawnPoint)
                DrawSpawnPoint(spawnPosition, spawnRotation);

            if (_drawValidationBox)
                DrawValidationBox(spawnPosition, spawnRotation);
        }

        private void DrawPlacementOrigin(Vector3 position)
        {
            Gizmos.color = _originColor;
            Gizmos.DrawWireSphere(position, 0.12f);
        }

        private void DrawSpawnPoint(Vector3 position, Quaternion rotation)
        {
            Gizmos.color = _spawnColor;
            Gizmos.DrawSphere(position, 0.08f);

            Gizmos.matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * 0.6f);
            Gizmos.DrawLine(Vector3.zero, Vector3.right * 0.3f);
            Gizmos.matrix = Matrix4x4.identity;
        }

        private void DrawValidationBox(Vector3 spawnPosition, Quaternion spawnRotation)
        {
            Vector3 center = spawnPosition + spawnRotation * _prop.ValidationBoxCenter;
            Vector3 size = _prop.ValidationBoxSize;

            Gizmos.color = _validationColor;
            Gizmos.matrix = Matrix4x4.TRS(center, spawnRotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, size);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}