using UnityEngine;

public abstract class AbstractPropPhysic : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected Rigidbody rb;
    [SerializeField] protected LayerMask groundMask;

    [Space(20)]

    [Header("Settings")]
    [SerializeField] protected float detectionDistance = 1f;
    [SerializeField] protected Vector3 offsetAdjustment;

    protected bool _isGrounded = true;

    protected abstract float SpawnOffsetY { get; }

    protected virtual void Awake()
    {
        _isGrounded = true;
    }

    protected Vector3 GetRaycastOrigin()
    {
        return transform.position - new Vector3(0, SpawnOffsetY, 0) + offsetAdjustment;
    }

    protected bool IsPropGrounded()
    {
        return Physics.Raycast(GetRaycastOrigin(), Vector3.down, detectionDistance, groundMask, QueryTriggerInteraction.Ignore);
    }

    protected virtual void HandlePropBehavior()
    {
        if (!_isGrounded) return;
        if (IsPropGrounded()) return;

        _isGrounded = false;
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    protected virtual void Update()
    {
        HandlePropBehavior();
    }

#if UNITY_EDITOR
    protected virtual void OnDrawGizmosSelected()
    {
        if (!rb) return;

        Gizmos.color = IsPropGrounded() ? Color.green : Color.red;
        Gizmos.DrawRay(GetRaycastOrigin(), Vector3.down * detectionDistance);
    }
#endif
}