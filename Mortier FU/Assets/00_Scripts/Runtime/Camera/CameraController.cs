using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;

namespace MortierFu
{
    public class CameraController : MonoBehaviour
    {
        enum ArenaCameraMode
        {
            FollowingPlayers,
            StaticMapView
        }

        [Header("Cinemachine")] [SerializeField]
        private CinemachineCamera _cinemachineCamera;

        [SerializeField] private CinemachineTargetGroup _targetGroup;
        [SerializeField] private CameraShakeController _shakeController;
        [SerializeField] private Transform _virtualTarget;
        [SerializeField] private Camera _camera;
        [SerializeField] private Camera _renderOnTopCamera;

        [SerializeField] private CinemachineCamera zoomCineCam;

        [SerializeField] private Vector3 _offset = new(3.5f, -1.2f, 0f);

        private float _currentOrthoSize;
        private Transform _endFightTarget;

        private SO_CameraSettings _cameraSettings;

        private bool _isStaticRaceCamera = false;

        private ArenaCameraMode _arenaMode = ArenaCameraMode.FollowingPlayers;
        private bool _isArenaMap;

        private LevelSystem _levelSystem;

        public Camera Camera => _camera;

        private void Start()
        {
            _currentOrthoSize = _cinemachineCamera.Lens.OrthographicSize;

            _cameraSettings = SystemManager.Instance.Get<CameraSystem>().Settings;
            _levelSystem = SystemManager.Instance.Get<LevelSystem>();
        }

        public void SetArenaMode(bool isArena)
        {
            _isArenaMap = isArena;
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

        private void ClearTargetGroupMember()
        {
            for (int i = _targetGroup.Targets.Count - 1; i >= 0; i--)
            {
                var t = _targetGroup.Targets[i].Object;
                if (t != null)
                    _targetGroup.RemoveMember(t);
            }

            if (_isStaticRaceCamera) return;

            _virtualTarget.position = _levelSystem.CurrentCameraMapConfig.PositionForRace;
            _currentOrthoSize = _levelSystem.CurrentCameraMapConfig.OrthoSize;
            ApplyLens();
        }

        private void UpdateCameraForTargets()
        {
            Bounds bounds = CalculateTargetsBounds();
            float extent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);

            if (_isArenaMap)
            {
                HandleArenaFallback(bounds.center, extent);
                return;
            }

            FollowPlayers(bounds.center, extent);
        }

        private void HandleArenaFallback(Vector3 center, float extent)
        {
            if (_arenaMode == ArenaCameraMode.FollowingPlayers &&
                extent > _cameraSettings.ArenaStopFollowExtent)
            {
                _arenaMode = ArenaCameraMode.StaticMapView;
            }
            else if (_arenaMode == ArenaCameraMode.StaticMapView &&
                     extent < _cameraSettings.ArenaResumeFollowExtent)
            {
                _arenaMode = ArenaCameraMode.FollowingPlayers;
            }

            if (_arenaMode == ArenaCameraMode.StaticMapView)
            {
                _virtualTarget.position = Vector3.Lerp(
                    _virtualTarget.position,
                    _levelSystem.CurrentCameraMapConfig.PositionForMap,
                    Time.deltaTime * _cameraSettings.PositionLerpSpeed
                );

                _currentOrthoSize = Mathf.Lerp(
                    _currentOrthoSize,
                    _levelSystem.CurrentCameraMapConfig.OrthoSize,
                    Time.deltaTime * _cameraSettings.ZoomLerpSpeed
                );

                ApplyLens();
            }
            else
            {
                FollowPlayers(center, extent);
            }
        }

        private void FollowPlayers(Vector3 center, float extent)
        {
            _virtualTarget.position = Vector3.Lerp(
                _virtualTarget.position,
                center,
                Time.deltaTime * _cameraSettings.PositionLerpSpeed
            );

            float clampedExtent = Mathf.Clamp(
                extent,
                _cameraSettings.MinPlayersExtent,
                _cameraSettings.MaxPlayersExtent
            );

            float t = Mathf.InverseLerp(
                _cameraSettings.MinPlayersExtent,
                _cameraSettings.MaxPlayersExtent,
                clampedExtent
            );

            float targetOrtho = Mathf.Lerp(
                _cameraSettings.MinOrthoSize,
                _cameraSettings.MaxOrthoSize,
                t
            );

            _currentOrthoSize = Mathf.Lerp(
                _currentOrthoSize,
                targetOrtho,
                Time.deltaTime * _cameraSettings.ZoomLerpSpeed
            );

            ApplyLens();
        }

        public async UniTask ApplyCameraMapConfigAsync(float tolerance = 0.01f,
            float maxWaitSeconds = 2f)
        {
            _isStaticRaceCamera = true;
            ClearTargetGroupMember();

            _virtualTarget.position = _levelSystem.CurrentCameraMapConfig.PositionForRace;
            _currentOrthoSize = _levelSystem.CurrentCameraMapConfig.OrthoSize;

            ApplyLens();

            float elapsed = 0f;

            while (true)
            {
                Vector3 camPos = _cinemachineCamera.transform.position;
                float camOrtho = _cinemachineCamera.Lens.OrthographicSize;

                bool positionReached = Vector3.Distance(camPos, _levelSystem.CurrentCameraMapConfig.PositionForRace) <=
                                       tolerance;
                bool orthoReached = Mathf.Abs(camOrtho - _levelSystem.CurrentCameraMapConfig.OrthoSize) <= tolerance;

                if (positionReached && orthoReached)
                    break;

                await UniTask.Yield();
                elapsed += Time.deltaTime;

                if (elapsed > maxWaitSeconds)
                {
                    Debug.LogWarning(
                        "[CameraController] ApplyCameraMapConfigAsync: Camera did not reach target in time");
                    break;
                }
            }
        }

        private void ResetCameraInstant()
        {
            if (_isArenaMap)
            {
                _virtualTarget.position = _levelSystem.CurrentCameraMapConfig.PositionForMap;
                _currentOrthoSize = _levelSystem.CurrentCameraMapConfig.OrthoSize;
                ApplyLens();
                return;
            }

            if (!_targetGroup.IsEmpty)
            {
                Bounds b = CalculateTargetsBounds();
                _virtualTarget.position = b.center;
            }

            float midOrtho = Mathf.Lerp(
                _cameraSettings.MinOrthoSize,
                _cameraSettings.MaxOrthoSize,
                0.5f
            );

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

        private void InitEndFightTarget()
        {
            if (_endFightTarget != null) return;
            GameObject go = new("EndFightCameraTarget");
            _endFightTarget = go.transform;
        }

        public void EndFightCameraMovement(Transform playerWin, float zoomDuration = 1f)
        {
            InitEndFightTarget();

            _endFightTarget.position = playerWin.position + _offset;

            zoomCineCam.Target.TrackingTarget = _endFightTarget;
            zoomCineCam.gameObject.SetActive(true);

            float startOrtho = _cinemachineCamera.Lens.OrthographicSize;
            float targetOrtho = startOrtho * 0.7f;
            float elapsed = 0f;

            while (elapsed < zoomDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / zoomDuration);

                float newOrtho = Mathf.Lerp(startOrtho, targetOrtho, t);
                _cinemachineCamera.Lens.OrthographicSize = newOrtho;
                _renderOnTopCamera.orthographicSize = newOrtho;
            }
        }

        public void ResetToMainCamera()
        {
            zoomCineCam.gameObject.SetActive(false);
        }
    }
}