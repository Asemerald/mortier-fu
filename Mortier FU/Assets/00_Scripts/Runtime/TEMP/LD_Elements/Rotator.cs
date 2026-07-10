using System;
using UnityEngine;

public class Rotator : MonoBehaviour
{
    [SerializeField] private float _speed = 12f;
    [SerializeField] private bool calculatePhysics;
    private Rigidbody _rb;
    [SerializeField] private bool canMoveInLoading;

    public Vector3 TransposePoint(Vector3 localPoint, float time)
    {
        var angle = time * _speed;
        Quaternion rotation = Quaternion.Euler(0f, angle, 0f);

        canMoveInLoading = true;
        
        return transform.position + rotation * localPoint;
    }

    private void Start()
    {
        if (calculatePhysics)
        {
            _rb = GetComponent<Rigidbody>();
        }
    }

    void FixedUpdate()
    {
        if (!canMoveInLoading)
            return;
        
        if (calculatePhysics)
        {
            _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, _speed * Time.deltaTime,0f ));
        }
        else
        {
            transform.Rotate(0,1 * Time.deltaTime * _speed,0);
        }
        
    }
}
