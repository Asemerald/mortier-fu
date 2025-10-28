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
    private float playerDist;

    [SerializeField] private AnimationCurve fovCurve;

    private void Start()
    {
        cameraShake = GetComponent<TEMP_CameraShake>();
    }

    private void Update()
    {
        var dist = Vector3.Distance(_targetGroup.transform.position, _targetGroup.Targets[0].Object.position);
        playerDist = Mathf.Lerp(playerDist, dist, Time.deltaTime * 4);
        Logs.Log($"Distance : {playerDist}");
        
        switch (playerDist)
        {
            case < 7 :
                break;
            case < 15 :
                cinemachineCamera.Lens.OrthographicSize = 18 - ((15 - playerDist)/1.6f) + cameraShake.addedFOV;
                _targetGroup.enabled = true;
                break;
            default :
                cinemachineCamera.Lens.OrthographicSize = 18 + cameraShake.addedFOV;
                _targetGroup.enabled = false;
                _targetGroup.transform.position = Vector3.Lerp(_targetGroup.transform.position, Vector3.zero, Time.deltaTime);
                break;
        }
    }
}
