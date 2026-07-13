using MortierFu.Shared;
using NaughtyAttributes;
using UnityEngine;

namespace MortierFu
{
    public class AimWidget : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private LayerMask _whatIsGround;
        [SerializeField] private float _raycastStartHeight = 15.0f;
        [SerializeField] private float _raycastMaxLength = 30.0f;
        [SerializeField] private float _resolvedHeightOffset = 0.05f;

        [Space]
        [ReadOnly] public Vector3 Origin;
        [ReadOnly] public Transform Target;
        [ReadOnly] public bool IsActive;
        [ReadOnly] public bool AttachedToTarget;

        [Header("Resolved Positions")]
        [ReadOnly] public Vector3 ImpactPosition;
        [ReadOnly] public bool HasImpactPosition;

        [SerializeField] private Vector3 _relativePosition;

        private MeshRenderer _meshRenderer;
        private Material _materialInstance;

        public Vector3 RelativePosition => _relativePosition;

        public Vector3 ShootTargetPosition =>
            HasImpactPosition ? ImpactPosition : transform.position;
        
        public Material MaterialInstance => _materialInstance;

        private static readonly int NormalizedAngleID = Shader.PropertyToID("_NormalizedAngle");

        private void Awake()
        {
            EnsureMaterialInstance();
            Hide();
        }

        private void OnDisable()
        {
            IsActive = false;
            HasImpactPosition = false;
        }

        private void Update()
        {
            ComputePosition();
        }

        private bool EnsureMaterialInstance()
        {
            if (_materialInstance)
                return true;

            if (!_meshRenderer)
            {
                _meshRenderer = GetComponent<MeshRenderer>();

                if (!_meshRenderer)
                    _meshRenderer = GetComponentInChildren<MeshRenderer>(true);
            }

            if (!_meshRenderer)
            {
                Logs.LogError("[AimWidget] Missing MeshRenderer on AimWidget or children.", this);
                return false;
            }

            _materialInstance = new Material(_meshRenderer.sharedMaterial);
            _meshRenderer.sharedMaterial = _materialInstance;

            return true;
        }

        public void SetRelativePosition(Vector3 relativePos)
        {
            _relativePosition = relativePos.With(y: 0f);
            ComputePosition();
        }

        public void RefreshPosition()
        {
            ComputePosition();
        }

        private void ComputePosition()
        {
            if (!IsActive)
                return;

            if (AttachedToTarget && Target)
                Origin = Target.position;

            Vector3 wantedGroundPosition = Origin + _relativePosition.With(y: 0f);

            Vector3 rayStartPos = wantedGroundPosition.With(y: _raycastStartHeight);
            Ray ray = new(rayStartPos, Vector3.down);

            if (Physics.Raycast(ray, out RaycastHit hit, _raycastMaxLength, _whatIsGround))
            {
                ImpactPosition = hit.point;
                HasImpactPosition = true;

                transform.position = hit.point.Add(y: _resolvedHeightOffset);
                return;
            }

            HasImpactPosition = false;
            ImpactPosition = wantedGroundPosition;
            transform.position = wantedGroundPosition;
        }

        public void Show()
        {
            IsActive = true;
            gameObject.SetActive(true);
            ComputePosition();
        }

        public void Hide()
        {
            IsActive = false;
            HasImpactPosition = false; gameObject.SetActive(false);
        }

        public void UpdateFireRateProgress(float progress)
        {
            if (!EnsureMaterialInstance())
                return;

            _materialInstance.SetFloat(NormalizedAngleID, progress);
        }

        public void Colorize(Color color)
        {
            if (!EnsureMaterialInstance())
                return;

            _materialInstance.color = color;
        }

        private void OnDestroy()
        {
            if (_materialInstance == null) return;
            Destroy(_materialInstance);
            _materialInstance = null;
        }
    }
}