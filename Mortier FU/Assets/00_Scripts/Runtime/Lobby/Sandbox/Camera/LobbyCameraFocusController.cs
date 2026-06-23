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
        [SerializeField] private Transform _settingsView;

        [Header("Movement")]
        [SerializeField] private float _moveDuration = 0.5f;

        private Coroutine _moveRoutine;

        private void Start()
        {
            MoveTo(_sandboxView);
        }

        public void FocusSandbox()
        {
            MoveTo(_sandboxView);
        }

        public void FocusSettings()
        {
            MoveTo(_settingsView);
        }

        private void MoveTo(Transform target)
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

            _moveRoutine = StartCoroutine(MoveRoutine(target));
        }

        private IEnumerator MoveRoutine(Transform target)
        {
            Vector3 startPosition = _cameraRig.position;
            Quaternion startRotation = _cameraRig.rotation;

            Vector3 targetPosition = target.position;
            Quaternion targetRotation = target.rotation;

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

                yield return null;
            }

            _cameraRig.SetPositionAndRotation(targetPosition, targetRotation);
            _moveRoutine = null;
        }
    }
}