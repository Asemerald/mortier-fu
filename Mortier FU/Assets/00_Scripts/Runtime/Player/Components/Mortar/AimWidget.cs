using MortierFu.Shared;
using NaughtyAttributes;
using UnityEngine;

namespace MortierFu
{
    /// <summary>
    /// Too much computes for a simple widget, but it will do for now.
    /// </summary>
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

        [SerializeField] private Vector3 _relativePosition;

        private MeshRenderer _meshRenderer;
        private Material _materialInstance;

        public Vector3 RelativePosition => _relativePosition;

        private static readonly int NormalizedAngleID = Shader.PropertyToID("_NormalizedAngle");

        private void Awake()
        {
            EnsureMaterialInstance();
            Hide();
        }

        private void OnDisable()
        {
            IsActive = false;
        }

        private void Update()
        {
            ComputePosition();
        }

        private bool EnsureMaterialInstance()
        {
            if (_materialInstance != null)
                return true;

            if (_meshRenderer == null)
            {
                _meshRenderer = GetComponent<MeshRenderer>();

                if (_meshRenderer == null)
                {
                    _meshRenderer = GetComponentInChildren<MeshRenderer>(true);
                }
            }

            if (_meshRenderer == null)
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
            _relativePosition = relativePos;
            ComputePosition();
        }

        private void ComputePosition()
        {
            if (!IsActive)
                return;

            if (AttachedToTarget && Target)
            {
                Origin = Target.position;
            }

            Vector3 newPos = Origin + _relativePosition;

            var rayStartPos = newPos.With(y: _raycastStartHeight);
            var ray = new Ray(rayStartPos, Vector3.down);

            if (Physics.Raycast(ray, out RaycastHit hit, _raycastMaxLength, _whatIsGround))
            {
                _relativePosition = hit.point.Add(y: _resolvedHeightOffset) - Origin;
            }

            transform.position = Origin + _relativePosition;
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
            gameObject.SetActive(false);
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
            if (_materialInstance != null)
            {
                Destroy(_materialInstance);
                _materialInstance = null;
            }
        }
    }
}