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
        [Header("Settings")] [SerializeField] private LayerMask _whatIsGround;
        [SerializeField] private float _raycastStartHeight = 15.0f;
        [SerializeField] private float _raycastMaxLength = 30.0f;
        [SerializeField] private float _resolvedHeightOffset = 0.05f;
        
        [Space]
        
        // Note: Globally hard set to given height and computed at different places (including strategies) CAN BE IMPROVED
        // Privacy is not relevant as this object is meant to be manipulated by the mortar
        [ReadOnly] public Vector3 Origin;
        [ReadOnly] public Transform Target;
        [ReadOnly] public bool IsActive;
        [ReadOnly] public bool AttachedToTarget;
        
        [SerializeField] Vector3 _relativePosition;
        
        public Vector3 RelativePosition => _relativePosition;

        private void Start()
        {
            Hide();
        }

        void Update()
        {
            ComputePosition();
        }
        
        public void SetRelativePosition(Vector3 relativePos)
        {
            _relativePosition = relativePos;
            ComputePosition();
        }
        
        private void ComputePosition()
        {
            if (!IsActive) return;

            if(AttachedToTarget && Target)
            {
                Origin = Target.position;
            }

            Vector3 newPos = Origin + _relativePosition;
            
            // Raycast down to find the ground position
            
            var rayStartPos = newPos.With(y: _raycastStartHeight);
            var ray = new Ray(rayStartPos, Vector3.down);
            
            if (Physics.Raycast(ray, out RaycastHit hit, _raycastMaxLength, _whatIsGround))
            {
                _relativePosition = hit.point.Add(y: _resolvedHeightOffset) - Origin;
            }
            
            transform.position = Origin + _relativePosition;
        }
        
        public void Show() => gameObject.SetActive(true);
        
        public void Hide() => gameObject.SetActive(false);
        
        public void Colorize(Color color) {
            if (TryGetComponent(out Renderer rend)) {
                rend.material.color = color;
            }
        }
    }
}