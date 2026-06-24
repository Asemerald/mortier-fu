using System;
using System.Collections;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbyCameraFocusController : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private Transform _cameraRig;

        [Header("Views")]
        [SerializeField] private Transform _sandboxView;
        [SerializeField] private float _sandboxFov;
        [SerializeField] private Transform _settingsView;
        [SerializeField] private float _settingsboxFov;

        [Header("Movement")]
        [SerializeField] private float _moveDuration = 0.5f;

        private Coroutine _moveRoutine;

        private void Start()
        {
            MoveTo(_sandboxView, _sandboxFov);
        }

        public void FocusSandbox()
        {
            MoveTo(_sandboxView,_sandboxFov);
        }

        public void FocusSettings()
        {
            MoveTo(_settingsView,_settingsboxFov);
        }

        private void MoveTo(Transform target, float fovSize)
        {
            if (_cameraRig == null)
            {
                Logs.LogError("[LobbyCameraFocusController] Camera rig reference is missing.");
                return;
            }

            if (target == null)
            {
                Logs.LogError("[LobbyCameraFocusController] Target view reference is missing.");
                return;
            }

            if (_moveRoutine != null)
            {
                StopCoroutine(_moveRoutine);
            }

            _moveRoutine = StartCoroutine(MoveRoutine(target,fovSize));
        }

        private IEnumerator MoveRoutine(Transform target, float fovSize)
        {
            Vector3 startPosition = _cameraRig.position;
            Quaternion startRotation = _cameraRig.rotation;
            float startSize = _cameraRig.gameObject.GetComponent<Camera>().orthographicSize;
            
            Vector3 targetPosition = target.position;
            Quaternion targetRotation = target.rotation;
            float targetSize = fovSize;

            if (_moveDuration <= 0f)
            {
                _cameraRig.SetPositionAndRotation(targetPosition, targetRotation);
                _moveRoutine = null;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < _moveDuration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / _moveDuration);
                t = Mathf.SmoothStep(0f, 1f, t);

                _cameraRig.position = Vector3.Lerp(startPosition, targetPosition, t);
                _cameraRig.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                _cameraRig.gameObject.GetComponent<Camera>().orthographicSize  = Mathf.Lerp(startSize, targetSize, t);
                

                yield return null;
            }

            _cameraRig.SetPositionAndRotation(targetPosition, targetRotation);
            _moveRoutine = null;
        }
    }
}