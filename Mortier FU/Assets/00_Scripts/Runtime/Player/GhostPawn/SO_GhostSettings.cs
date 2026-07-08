using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(
        fileName = "DA_GhostSettings",
        menuName = "Mortier Fu/Settings/Ghost"
    )]
    public sealed class SO_GhostSettings : ScriptableObject
    {
        [Header("Spawn")] [Min(0f)] [SerializeField]
        private float _spawnDelay = 2f;

        [SerializeField] private PlayerGhostPawn _ghostPawnPrefab;

        [Header("Movement")] [Min(0f)] [SerializeField]
        private float _moveSpeed = 5f;

        [Min(0f)] [SerializeField] private float _acceleration = 8f;

        [Min(0f)] [SerializeField] private float _deceleration = 6f;

        [Min(0f)] [SerializeField] private float _floatHeight = 0.45f;

        [Min(0.01f)] [SerializeField] private float _collisionRadius = 0.35f;

        [Min(0.01f)] [SerializeField] private float _groundRaycastStartHeight = 3f;

        [Min(0.01f)] [SerializeField] private float _groundRaycastLength = 8f;
        
        [Min(0.01f)] [SerializeField] private float _waterRaycastLenght = 3f;
        [Min(0.01f)] [SerializeField] private float _waterRaycastHeightMargin = .3f;



        [Header("Spawn Resolving")]
        [Min(0.05f)]
        [SerializeField] private float _spawnCheckRadius = 0.45f;

        [Min(0.25f)]
        [SerializeField] private float _vehicleSpawnSearchRadius = 4f;

        [Min(4)]
        [SerializeField] private int _vehicleSpawnSearchSteps = 16;

        [SerializeField] private LayerMask _ghostSpawnBlockingMask;
        
        [Header("Layers")] [SerializeField] private LayerMask _groundMask;
        [SerializeField] private LayerMask _ghostBoundaryMask;
        [SerializeField] private LayerMask _placementBlockingMask;
        [SerializeField] private LayerMask _waterLayerMask;

        [Header("Prop Placement")] [Min(0f)] [SerializeField]
        private float _propSpawnCooldown = 1f;

        [SerializeField] private SO_GhostPlaceableProp[] _placeableProps;

        [Header("Preview Materials")] 
        [SerializeField] private Material _validPreviewMaterial;
        [SerializeField] private Material _invalidPreviewMaterial;

        public float SpawnDelay => _spawnDelay;
        public PlayerGhostPawn GhostPawnPrefab => _ghostPawnPrefab;

        public float MoveSpeed => _moveSpeed;
        public float Acceleration => _acceleration;
        public float Deceleration => _deceleration;
        public float FloatHeight => _floatHeight;
        public float CollisionRadius => _collisionRadius;
        public float GroundRaycastStartHeight => _groundRaycastStartHeight;
        public float GroundRaycastLength => _groundRaycastLength;
        public float WaterRaycastLength => _waterRaycastLenght;
        public float WaterRaycastHeightMargin => _waterRaycastHeightMargin;
        
        public float SpawnCheckRadius => _spawnCheckRadius;
        public float VehicleSpawnSearchRadius => _vehicleSpawnSearchRadius;
        public int VehicleSpawnSearchSteps => _vehicleSpawnSearchSteps;
        public LayerMask GhostSpawnBlockingMask => _ghostSpawnBlockingMask;
        
        public LayerMask GroundMask => _groundMask;
        public LayerMask GhostBoundaryMask => _ghostBoundaryMask;
        public LayerMask PlacementBlockingMask => _placementBlockingMask;
        public LayerMask WaterLayerMask  => _waterLayerMask;

        public float PropSpawnCooldown => _propSpawnCooldown;
        public SO_GhostPlaceableProp[] PlaceableProps => _placeableProps;
        
        public Material ValidPreviewMaterial => _validPreviewMaterial;
        public Material InvalidPreviewMaterial => _invalidPreviewMaterial;
    }
}