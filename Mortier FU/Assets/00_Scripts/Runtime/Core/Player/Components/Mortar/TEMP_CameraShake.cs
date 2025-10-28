using System;
using System.Collections;
using MortierFu.Shared;
using Unity.Cinemachine;
using UnityEngine;


public class TEMP_CameraShake : MonoBehaviour
{
    public static TEMP_CameraShake Instance { get; private set;}
    
    [SerializeField] private CinemachineCamera cinemachineCamera;
    private CinemachineBasicMultiChannelPerlin perlin;
    
    private float shakeTimer;
    private float shakeIntensity;
    private float shakeMult;
    
    private float zoomTimer;
    private float zoomValue;
    
    [SerializeField] private float defaultFOV = 60;
    private float targetFOV;
    private float currentFOV;
    
    [SerializeField] private AnimationCurve zoomCurve;
    
    private void Awake()
    {
        Instance = this;
        perlin = cinemachineCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
        
        targetFOV = defaultFOV;
        currentFOV = defaultFOV;
        cinemachineCamera.Lens.FieldOfView = defaultFOV;
    }
    
    private void Update()
    {
        ShakeUpdate();
        ZoomUpdate();
    }
    

    public void CallCameraShake(float aoeRange, float power, float travelTime, float delay = 0)
    {
        float intensity = (aoeRange * power * travelTime) /30;
        float time = intensity * 0.05f;
        Logs.Log($"intensity : {intensity} / time : {time}");
        StartCoroutine(ShakeCamera(intensity, time, intensity, time/2));
    }
    
    private IEnumerator ShakeCamera(float intensity, float shakeTime, float value, float zoomTime, float delay = 0)
    {
        yield return new WaitForSeconds(delay);
        shakeTimer = shakeTime;
        shakeIntensity = intensity;
        shakeMult = 1 / shakeTimer;
        
        zoomTimer = zoomTime;
        zoomValue = value;
    }

    private void ShakeUpdate()
    {
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
        }
        
        perlin.AmplitudeGain = (shakeIntensity * shakeMult) * shakeTimer;
        
        if (shakeTimer <= 0f)
        {
            //Time over
            perlin.AmplitudeGain = 0;
        }
    }
    
    private void ZoomUpdate()
    {
        if (zoomTimer > 0)
        {
            zoomTimer -= Time.deltaTime;
        }

        cinemachineCamera.Lens.FieldOfView = currentFOV + (zoomValue * zoomTimer);
        
        if (zoomTimer <= 0f)
        {
            //Time over
            cinemachineCamera.Lens.FieldOfView = currentFOV;
        }
    }
    
}
