using System;
using MortierFu.Shared;
using Unity.Cinemachine;
using UnityEngine;

public class TEMP_CameraHandler : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cinemachineCamera;
    private TEMP_CameraShake cameraShake;
    [Space(10)]
    
    [SerializeField] private CinemachineTargetGroup _targetGroup;
    [SerializeField] private Transform _target;
    
    private float playerDist;
    private Vector3 hideTarget;
    
    private float targetFov, targetOrthoSize;
    private float hideFov, hideOrthoSize;

    private void Start()
    {
        cameraShake = GetComponent<TEMP_CameraShake>();
        targetFov = 60;
        targetOrthoSize = 18;
        hideFov = 60;
        hideOrthoSize = 18;
    }

    private void Update()
    {
        MovementUpdate();
        TargetUpdate();
    }

    private void MovementUpdate()
    {
        float dist;
        if (!_targetGroup.IsEmpty)
        {
            dist = Vector3.Distance(_targetGroup.transform.position, _targetGroup.Targets[0].Object.position);
            playerDist = Mathf.Lerp(playerDist, dist, Time.deltaTime * 4);
        }
        else
        {
            playerDist = 0;
        }
        Logs.Log($"Distance : {targetOrthoSize}");
        
        switch (playerDist)
        {
            case < 7 :
                break;
            case < 15 :
                targetOrthoSize = 18 - ((15 - playerDist) / 1.6f);
                targetFov = 60  - (15 - playerDist);
                break;
            default :
                targetOrthoSize = 18;
                targetFov = 60;
                break;
        }

        hideFov = Mathf.Lerp(hideFov, targetFov, Time.deltaTime * 1);
        hideOrthoSize = Mathf.Lerp(hideOrthoSize, targetOrthoSize, Time.deltaTime * 1);
        
        cinemachineCamera.Lens.OrthographicSize = hideOrthoSize + cameraShake.addedFOV;     //ORTHO
        cinemachineCamera.Lens.FieldOfView = hideFov + cameraShake.addedFOV;                //PERSPECTIVE
    }

    private void TargetUpdate()
    {
        _target.position = Vector3.Lerp(_target.position, hideTarget, Time.deltaTime);
        
        if (playerDist < 15)
        {
            hideTarget = _targetGroup.transform.position;
        }
        else
        {
            hideTarget = new Vector3(0, -10, 0);
        }
    }
}
