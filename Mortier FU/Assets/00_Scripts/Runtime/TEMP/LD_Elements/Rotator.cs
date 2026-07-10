using System;
using MortierFu;
using UnityEngine;

public class Rotator : MonoBehaviour
{
    [SerializeField] private float _speed = 12f;
    [SerializeField] private bool calculatePhysics;
    private Rigidbody _rb;
    [SerializeField] private bool canMoveInLoading;
    private GameModeBase _gm;

    public Vector3 TransposePoint(Vector3 localPoint, float time)
    {
        var angle = time * _speed;
        Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
        
        
        canMoveInLoading = true;
        
        return transform.position + rotation * localPoint;
    }
    private void Awake()
    {
        _gm = GameService.CurrentGameMode as GameModeBase;
        
    }

    private void OnEnable()
    {
        if (calculatePhysics)
        {
            _rb = GetComponent<Rigidbody>();
        }

        if (_gm != null)
        {
            _gm.OnRacePlayerConfirmation += ActivateMovement;
            Debug.Log("gm good");
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

    private void ActivateMovement()
    {
        canMoveInLoading = true;
        Debug.Log("can move true");
    }
}
