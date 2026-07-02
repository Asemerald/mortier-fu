using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(
        fileName = "DA_GhostPlaceableProp",
        menuName = "Mortier Fu/Ghost/Ghost Placeable Prop"
    )]
    public sealed class SO_GhostPlaceableProp : ScriptableObject
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject _realPrefab;
        [SerializeField] private GameObject _previewPrefab;

        [Header("Placement")]
        [SerializeField] private Vector3 _spawnOffset;
        [SerializeField] private Vector3 _rotationEulerOffset;

        [Header("Validation")]
        [SerializeField] private Vector3 _validationBoxCenter = Vector3.zero;
        [SerializeField] private Vector3 _validationBoxSize = Vector3.one;

        public GameObject RealPrefab => _realPrefab;
        public GameObject PreviewPrefab => _previewPrefab ? _previewPrefab : _realPrefab;

        public Vector3 SpawnOffset => _spawnOffset;
        public Quaternion RotationOffset => Quaternion.Euler(_rotationEulerOffset);

        public Vector3 ValidationBoxCenter => _validationBoxCenter;
        public Vector3 ValidationBoxHalfExtents => _validationBoxSize * 0.5f;
    }
}