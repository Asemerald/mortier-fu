using UnityEngine;

namespace MortierFu
{
    public class AimWidget : MonoBehaviour
    {
        // Privacy is not relevant as this object is meant to be manipulated by the mortar
        public Vector3 RelativePosition;
        public Vector3 Origin;
        public Transform Target;
        public bool IsActive;
        public bool AttachedToTarget;
        
        void Update()
        {
            if (!IsActive) return;

            if(AttachedToTarget) {
                Origin = Target.position;
            }

            transform.position = Origin + RelativePosition;
        }

        public void Show() => gameObject.SetActive(true);
        
        public void Hide() => gameObject.SetActive(false);
    }
}