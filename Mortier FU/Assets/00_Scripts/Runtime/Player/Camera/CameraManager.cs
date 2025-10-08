using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _cinemachineCamera;
    private CinemachineBasicMultiChannelPerlin _perlin;
    private float _shakeTimer;
    private float _shakeIntensity;
    private float _shakeMult;
    
    [SerializeField] private CinemachineTargetGroup _cinemachineTargetGroup;
    
    public static CameraManager Instance { get; private set;}
    
    private void Awake()
    {
        Instance = this;
        _perlin = _cinemachineCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        ShakeUpdate();
    }

    public void AddPlayerToCameraView(GameObject player)
    {
        _cinemachineTargetGroup.AddMember(player.transform, 1, 1);
    }

    public void ShakeCamera(float intensity, float time, float delay = 0)
    {
        StartCoroutine(ShakeCoroutine(intensity, time, delay));
    }

    public IEnumerator ShakeCoroutine(float intensity, float time, float delay)
    {
        yield return new WaitForSeconds(delay);
        _shakeTimer = time;
        _shakeIntensity = intensity;
        _shakeMult = 1 / _shakeTimer;
    }
    
    private void ShakeUpdate()
    {
        if (_shakeTimer > 0)
        {
            _shakeTimer -= Time.deltaTime;
        }
        
        _perlin.AmplitudeGain = (_shakeIntensity * _shakeMult) * _shakeTimer;
        
        if (_shakeTimer <= 0f)
        {
            //Time over
            _perlin.AmplitudeGain = 0;
        }
    }
}
