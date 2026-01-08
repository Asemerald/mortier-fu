using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace MortierFu
{
    public class CameraShakeController : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera cinemachineCamera;
        private CinemachineBasicMultiChannelPerlin _perlin;

        private float _shakeTimer;
        private float _shakeIntensity;
        private float _shakeMult;

        private float _zoomTimer;
        private float _zoomValue;

        public float AddedFOV { get; private set; }

        private void Awake()
        {
            _perlin = cinemachineCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
        }

        private void Update()
        {
            ShakeUpdate();
            ZoomUpdate();
        }

        public void CallCameraShake(float aoeRange, float power, float travelTime, float delay = 0)
        {
            float intensity = Mathf.Clamp((aoeRange * power * travelTime) / 40, 1f, 10f);
            float time = intensity * 0.07f;
            StartCoroutine(ShakeCamera(intensity, time, intensity, time / 2f, delay));
        }

        public void CallZoomPulse(float value, float time)
        {
            _zoomTimer = time;
            _zoomValue = value;
        }

        private IEnumerator ShakeCamera(float intensity, float shakeTime, float value, float zoomTime, float delay = 0)
        {
            if (delay > 0)
                yield return new WaitForSeconds(delay);

            _shakeTimer = shakeTime;
            _shakeIntensity = intensity * 0.25f;
            _shakeMult = 1f / _shakeTimer * 0.5f;

            _zoomTimer = zoomTime * 0.5f;
            _zoomValue = value * 0.25f;
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
                _perlin.AmplitudeGain = 0;
            }
        }

        private void ZoomUpdate()
        {
            if (_zoomTimer > 0)
            {
                _zoomTimer -= Time.deltaTime;
            }

            AddedFOV = _zoomValue * _zoomTimer;

            if (_zoomTimer <= 0f)
            {
                AddedFOV = 0;
            }
        }
    }
}