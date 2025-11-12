using System;
using MortierFu.Shared;
using PrimeTween;
using UnityEngine;

public class TEMP_CutoutChecker : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private LayerMask _lm;

    private void Awake()
    {
        _camera = Camera.main;
    }

    private void Update()
    {
        RaycastHit hit;
        
        //Does the ray intersects with the mesh?
        if (Physics.Raycast(_camera.transform.position, (transform.position - _camera.transform.position).normalized, out hit, 1000))
        {
            if (hit.transform != transform.parent.parent)
            {
                if (transform.localScale == Vector3.zero)
                {
                    Tween.Scale(transform, transform.localScale, Vector3.one * 4, 0.5f);
                }
            }
            else
            {
                if (transform.localScale == Vector3.one * 4)
                {
                    Tween.Scale(transform, transform.localScale, Vector3.zero, 0.5f);
                }
            }
        }
    }
}
