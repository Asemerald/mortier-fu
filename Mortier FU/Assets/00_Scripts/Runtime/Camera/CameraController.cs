using Unity.Cinemachine;
using UnityEngine;

namespace MortierFu
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private CinemachineTargetGroup _targetGroup;
        [SerializeField] private CameraShakeController _shakeController;
        [SerializeField] private Transform _virtualTarget;

        private float _playerDist;
        private Vector3 _hideTarget;

        private float _targetFov = 60f;
        private float _targetOrthoSize = 18f;
        private float _hideFov = 60f;
        private float _hideOrthoSize = 18f;

        private void Update()
        {
            MovementUpdate();
            TargetUpdate();
        }

        public void AddTarget(Transform playerTransform, float weight = 1f, float radius = 0f)
        {
            if (playerTransform == null) return;
            _targetGroup.AddMember(playerTransform, weight, radius);
        }

        public void RemoveTarget(Transform playerTransform)
        {
            if (playerTransform == null) return;
            _targetGroup.RemoveMember(playerTransform);
        }

        public void ClearTargets()
        {
            foreach (var target in _targetGroup.Targets)
            {
                _targetGroup.RemoveMember(target.Object);
            }
        }

        public void ResetCameraInstant()
        {
            if (!_targetGroup.IsEmpty)
            {
                _virtualTarget.position = _targetGroup.transform.position;
                _hideTarget = _virtualTarget.position;
            }

            _hideFov = _targetFov = 60f;
            _hideOrthoSize = _targetOrthoSize = 18f;
        }

        public void ResetCameraSmooth(float duration)
        {
            ResetCameraInstant();
        }

        public void Shake(float aoeRange, float power, float travelTime, float delay = 0f)
        {
            if (_shakeController != null)
                _shakeController.CallCameraShake(aoeRange, power, travelTime, delay);
        }

        public void PunchZoom(float intensity, float duration)
        {
            if (_shakeController != null)
                _shakeController.CallZoomPulse(intensity, duration);
        }

        private void MovementUpdate()
        {
            float dist;
            if (!_targetGroup.IsEmpty)
            {
                dist = Vector3.Distance(_targetGroup.transform.position, _targetGroup.Targets[0].Object.position);
                _playerDist = Mathf.Lerp(_playerDist, dist, Time.deltaTime * 4);
            }
            else
            {
                _playerDist = 0;
            }

            switch (_playerDist)
            {
                case < 7:
                    break;
                case < 15:
                    _targetOrthoSize = 18 - ((15 - _playerDist) / 1.6f);
                    _targetFov = 60 - (15 - _playerDist);
                    break;
                default:
                    _targetOrthoSize = 18;
                    _targetFov = 60;
                    break;
            }

            _hideFov = Mathf.Lerp(_hideFov, _targetFov, Time.deltaTime);
            _hideOrthoSize = Mathf.Lerp(_hideOrthoSize, _targetOrthoSize, Time.deltaTime);

            _cinemachineCamera.Lens.OrthographicSize =
                _hideOrthoSize + (_shakeController != null ? _shakeController.AddedFOV : 0f);
            _cinemachineCamera.Lens.FieldOfView =
                _hideFov + (_shakeController != null ? _shakeController.AddedFOV : 0f);
        }

        private void TargetUpdate()
        {
            _virtualTarget.position = Vector3.Lerp(_virtualTarget.position, _hideTarget, Time.deltaTime);

            _hideTarget = _playerDist < 15 ? _targetGroup.transform.position : new Vector3(0, -10, 0);
        }
    }
}