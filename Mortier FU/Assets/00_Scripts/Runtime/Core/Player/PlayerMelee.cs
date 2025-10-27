using UnityEngine;

namespace MortierFu
{
    public class PlayerMelee : MonoBehaviour
    {
        [SerializeField] private float _viewAngle = 90f;
        [SerializeField] private float _viewDistance = 2f;
        [SerializeField] private Color _viewColor = Color.green;

        private void OnDrawGizmos()
        {
            Gizmos.color = _viewColor;

            if (!(_viewDistance > 0f)) return;

            var origin = transform.position;
            var forward = transform.forward;
            var halfAngle = _viewAngle * 0.5f;
            var leftDir = Quaternion.Euler(0f, -halfAngle, 0f) * forward;
            var rightDir = Quaternion.Euler(0f, halfAngle, 0f) * forward;

            Gizmos.DrawLine(origin, origin + leftDir * _viewDistance);
            Gizmos.DrawLine(origin, origin + rightDir * _viewDistance);

            var segments = 24;
            var prev = origin + leftDir * _viewDistance;
            for (var i = 1; i <= segments; i++)
            {
                var t = (float)i / segments;
                var angle = -halfAngle + t * _viewAngle;
                var dir = Quaternion.Euler(0f, angle, 0f) * forward;
                var next = origin + dir * _viewDistance;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}