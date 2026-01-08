using Unity.Cinemachine;
using UnityEngine;

namespace MortierFu
{
    public class CameraController : MonoBehaviour
    {
        [Header("Cinemachine")] [SerializeField]
        private CinemachineCamera _cinemachineCamera;

        [SerializeField] private CinemachineTargetGroup _targetGroup;
        [SerializeField] private CameraShakeController _shakeController;
        [SerializeField] private Transform _virtualTarget;
        [SerializeField] private Camera _camera;
        [SerializeField] private Camera _renderOnTopCamera;

        [SerializeField] private CinemachineCamera zoomCineCam;

        private float _currentOrthoSize;

        private SO_CameraSettings _cameraSettings;

        private bool _isStaticRaceCamera = false;

        public Camera Camera => _camera;

        private void Start()
        {
            _currentOrthoSize = _cinemachineCamera.Lens.OrthographicSize;

            _cameraSettings = SystemManager.Instance.Get<CameraSystem>().Settings;
        }

        private void LateUpdate()
        {
            if (_isStaticRaceCamera) return;

            if (!_targetGroup.IsEmpty)
                UpdateCameraForTargets();
        }

        public void PopulateTargetGroup(Transform[] playerTransforms, float weight = 1f, float radius = 0f)
        {
            if (playerTransforms == null) return;

            ClearTargetGroupMember();

            foreach (var t in playerTransforms)
            {
                if (t == null) continue;
                _targetGroup.AddMember(t, weight, radius);
            }

            ResetCameraInstant();

            _isStaticRaceCamera = false;
        }

        public void RemoveTarget(Transform playerTransform)
        {
            if (playerTransform == null) return;
            _targetGroup.RemoveMember(playerTransform);
        }

        public void ClearTargetGroupMember()
        {
            for (int i = _targetGroup.Targets.Count - 1; i >= 0; i--)
            {
                var t = _targetGroup.Targets[i].Object;
                if (t != null)
                    _targetGroup.RemoveMember(t);
            }

            if (_isStaticRaceCamera) return;

            _virtualTarget.position = _cameraSettings.RacePosition;
            _currentOrthoSize = _cameraSettings.DefaultOrtho;
            ApplyLens();
        }

        private void UpdateCameraForTargets()
        {
            Bounds bounds = CalculateTargetsBounds();
            Vector3 center = bounds.center;
            float extent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);

            _virtualTarget.position = Vector3.Lerp(
                _virtualTarget.position,
                center,
                Time.deltaTime * _cameraSettings.PositionLerpSpeed);

            float clampedExtent =
                Mathf.Clamp(extent, _cameraSettings.MinPlayersExtent, _cameraSettings.MaxPlayersExtent);
            float t = Mathf.InverseLerp(_cameraSettings.MinPlayersExtent, _cameraSettings.MaxPlayersExtent,
                clampedExtent);

            float targetOrtho = Mathf.Lerp(_cameraSettings.MinOrthoSize, _cameraSettings.MaxOrthoSize, t);

            _currentOrthoSize =
                Mathf.Lerp(_currentOrthoSize, targetOrtho, Time.deltaTime * _cameraSettings.ZoomLerpSpeed);

            ApplyLens();
        }

        public void ApplyCameraMapConfig(CameraMapConfig mapConfig)
        {
            _isStaticRaceCamera = true;

            _virtualTarget.localPosition = mapConfig.PositionForRace;
            _currentOrthoSize = mapConfig.OrthoSize;

            ApplyLens();
        }

        private void ResetCameraInstant()
        {
            if (!_targetGroup.IsEmpty)
            {
                Bounds b = CalculateTargetsBounds();
                _virtualTarget.position = b.center;
            }

            float midOrtho = Mathf.Lerp(_cameraSettings.MinOrthoSize, _cameraSettings.MaxOrthoSize, 0.5f);

            _currentOrthoSize = midOrtho;

            ApplyLens();
        }

        public void Shake(float aoeRange, float power, float travelTime, float delay = 0f)
        {
            _shakeController?.CallCameraShake(aoeRange, power, travelTime, delay);
        }

        public void PunchZoom(float intensity, float duration)
        {
            _shakeController?.CallZoomPulse(intensity, duration);
        }

        private void ApplyLens()
        {
            float shakeFovOffset = _shakeController != null ? _shakeController.AddedFOV : 0f;
            float newOrthographicSize = _currentOrthoSize + shakeFovOffset;

            _cinemachineCamera.Lens.OrthographicSize = newOrthographicSize;
            _renderOnTopCamera.orthographicSize = newOrthographicSize;
        }

        private Bounds CalculateTargetsBounds()
        {
            var targets = _targetGroup.Targets;

            bool firstFound = false;
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

            foreach (var t in targets)
            {
                if (t.Object == null) continue;

                if (!firstFound)
                {
                    bounds = new Bounds(t.Object.position, Vector3.zero);
                    firstFound = true;
                }
                else
                {
                    bounds.Encapsulate(t.Object.position);
                }
            }

            if (!firstFound)
            {
                bounds = new Bounds(_virtualTarget.position, Vector3.one);
            }

            return bounds;
        }

        public void EndFightCameraMovement(Transform playerWin)
        {
            zoomCineCam.Target.TrackingTarget = playerWin;
            zoomCineCam.gameObject.SetActive(true);
            
            //ADD SLOW MO EFFECT
            
        }

        public void ResetToMainCamera()
        {
            zoomCineCam.gameObject.SetActive(false);
        }
        
        private void DebugDrawBounds(Bounds b, Color c)
        {
            Vector3 v3FrontTopLeft = new Vector3(b.min.x, b.max.y, b.max.z);
            Vector3 v3FrontTopRight = new Vector3(b.max.x, b.max.y, b.max.z);
            Vector3 v3FrontBottomLeft = new Vector3(b.min.x, b.min.y, b.max.z);
            Vector3 v3FrontBottomRight = new Vector3(b.max.x, b.min.y, b.max.z);
            Vector3 v3BackTopLeft = new Vector3(b.min.x, b.max.y, b.min.z);
            Vector3 v3BackTopRight = new Vector3(b.max.x, b.max.y, b.min.z);
            Vector3 v3BackBottomLeft = new Vector3(b.min.x, b.min.y, b.min.z);
            Vector3 v3BackBottomRight = new Vector3(b.max.x, b.min.y, b.min.z);

            Debug.DrawLine(v3FrontTopLeft, v3FrontTopRight, c);
            Debug.DrawLine(v3FrontTopRight, v3FrontBottomRight, c);
            Debug.DrawLine(v3FrontBottomRight, v3FrontBottomLeft, c);
            Debug.DrawLine(v3FrontBottomLeft, v3FrontTopLeft, c);

            Debug.DrawLine(v3BackTopLeft, v3BackTopRight, c);
            Debug.DrawLine(v3BackTopRight, v3BackBottomRight, c);
            Debug.DrawLine(v3BackBottomRight, v3BackBottomLeft, c);
            Debug.DrawLine(v3BackBottomLeft, v3BackTopLeft, c);

            Debug.DrawLine(v3FrontTopLeft, v3BackTopLeft, c);
            Debug.DrawLine(v3FrontTopRight, v3BackTopRight, c);
            Debug.DrawLine(v3FrontBottomRight, v3BackBottomRight, c);
            Debug.DrawLine(v3FrontBottomLeft, v3BackBottomLeft, c);
        }
    }
}