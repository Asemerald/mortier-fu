using UnityEngine;

namespace MortierFu
{
    /// <summary>
    /// Too much computes for a simple widget, but it will do for now.
    /// </summary>
    public class AimWidget : MonoBehaviour
    {
        // Privacy is not relevant as this object is meant to be manipulated by the mortar
        public Vector3 Origin;
        public Transform Target;
        public bool IsActive;
        public bool AttachedToTarget;
        
        private Vector3 _relativePosition;
        
        public Vector3 RelativePosition => _relativePosition;

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

            if(AttachedToTarget && Target) {
                Origin = Target.position;
            }

            transform.position = Origin + _relativePosition;
        }

        public void Show() => gameObject.SetActive(true);
        
        public void Hide() => gameObject.SetActive(false);
    }
}