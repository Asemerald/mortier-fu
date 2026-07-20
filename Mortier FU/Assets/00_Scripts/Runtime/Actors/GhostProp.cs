using System;
using MortierFu;
using UnityEngine;

public class GhostProp : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SO_GhostPlaceableProp settings;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private LayerMask groundMask;
    
    [Space(20)]
    
    [Header("Settings")]
    [SerializeField] private float detectionDistance = 1f;
    [SerializeField] private Vector3 offsetAdjustment;
    
    [SerializeField] private bool _isGrounded = true;

    private void Awake()
    {
        _isGrounded = true;
        rb.isKinematic = true;
        rb.useGravity = true;
    }

    private bool IsPropGrounded()
    {
        Vector3 startPosition = transform.position - new Vector3(0, settings.SpawnOffset.y, 0) + offsetAdjustment;
        return Physics.Raycast(startPosition, Vector3.down, detectionDistance,groundMask,QueryTriggerInteraction.Ignore);
    }

    private void HandleGhostPropBehavior()
    {
        if (!_isGrounded) return;
        if (IsPropGrounded()) return;
        
        _isGrounded = false;
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    private void Update()
    {
        HandleGhostPropBehavior();
    }

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        if (!rb || !settings) return;
        
        Gizmos.color = IsPropGrounded() ? Color.green : Color.red;

        Vector3 startPosition = transform.position - new Vector3(0, settings.SpawnOffset.y, 0) + offsetAdjustment;
        
        Gizmos.DrawRay(startPosition, Vector3.down * detectionDistance);
    }

#endif
}
