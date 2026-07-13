using System.Numerics;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;
using System.Threading;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

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

        [SerializeField] private BoxCollider _boundbox;
        [SerializeField] private float _dampingBound;

        [SerializeField] private Vector3 _offset = new(3.5f, -1.2f, 0f);

        private float _actualLerpSpeed;
        private float _currentOrthoSize;
        private float _maxArenaOrthoSize;
        private Transform _endFightTarget;

        private SO_CameraSettings _cameraSettings;

        private bool _isStaticRaceCamera = false;

        private ArenaCameraMode _arenaMode = ArenaCameraMode.FollowingPlayers;
        private bool _isArenaMap;

        private LevelSystem _levelSystem;

        public Camera Camera => _camera;

        private bool _isInLobby = true;

        private void Start()
        {
            _currentOrthoSize = _cinemachineCamera.Lens.OrthographicSize;

            _cameraSettings = SystemManager.Instance.Get<CameraSystem>().Settings;
            _levelSystem = SystemManager.Instance.Get<LevelSystem>();
            
            // if current scene is Lobby, set location to Vector3(-13,30.3999996,-2)
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Lobby")
            {
                transform.position = new Vector3(-13, 30.3999996f, -2);
                ApplyLens();
                _isInLobby = true;
            }
            
            // listen for scene change and flip the bool
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += (prev, next) =>
            {
                if (next.name != "Lobby")
                {
                    _isInLobby = false;
                }
            };
            
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
            if (_isInLobby) return;
            
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
                _virtualTarget.position = Camera.transform.position -
                                          _cinemachineCamera.GetComponent<CinemachineFollow>().FollowOffset;
            }
            else if (_arenaMode == ArenaCameraMode.StaticMapView &&
                     extent < _cameraSettings.ArenaResumeFollowExtent)
            {
                _actualLerpSpeed = _cameraSettings.MinPositionLerpSpeed;
                _arenaMode = ArenaCameraMode.FollowingPlayers;
            }

            HandleBoundBox();

            if (_arenaMode == ArenaCameraMode.StaticMapView)
            {
                _virtualTarget.position = Vector3.Lerp(
                    _virtualTarget.position,
                    _levelSystem.CurrentCameraMapConfig.PositionForMap,
                    Time.deltaTime * _cameraSettings.MinPositionLerpSpeed
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

        private void HandleBoundBox()
        {
            _boundbox.size = new Vector3((_maxArenaOrthoSize - _currentOrthoSize + _dampingBound) * _camera.aspect,
                _maxArenaOrthoSize - _currentOrthoSize + _dampingBound, 1);
        }

        private void FollowPlayers(Vector3 center, float extent)
        {
            _actualLerpSpeed = Mathf.Lerp(_actualLerpSpeed, _cameraSettings.MaxPositionLerpSpeed,
                Time.deltaTime * _cameraSettings.LerpSpeed);

            _virtualTarget.position = Vector3.Lerp(
                _virtualTarget.position,
                center,
                Time.deltaTime * _actualLerpSpeed
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
                _levelSystem.CurrentCameraMapConfig.OrthoSize,
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

            Vector3 followOffset = _cinemachineCamera.GetComponent<CinemachineFollow>().FollowOffset;
            Vector3 camTargetPos = _virtualTarget.position + followOffset;

            ApplyLens();

            float elapsed = 0f;

            while (true)
            {
                Vector3 camPos = _cinemachineCamera.transform.position;
                float camOrtho = _cinemachineCamera.Lens.OrthographicSize;

                bool positionReached = Vector3.Distance(camPos, camTargetPos) <= tolerance;
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

        public void ApplyRaceCameraMapConfigInstant()
        {
            if (!_cinemachineCamera || !_virtualTarget || _levelSystem == null)
                return;

            _isStaticRaceCamera = true;

            ClearTargetGroupMember();

            _virtualTarget.position = _levelSystem.CurrentCameraMapConfig.PositionForRace;
            _currentOrthoSize = _levelSystem.CurrentCameraMapConfig.OrthoSize;

            SetupBoundBox(_levelSystem.CurrentCameraMapConfig.PositionForRace, _currentOrthoSize);

            ApplyLens();

            Vector3 cameraPosition = GetCameraPositionForVirtualTarget();
            Quaternion cameraRotation = _cinemachineCamera.transform.rotation;

            _cinemachineCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
            _cinemachineCamera.ForceCameraPosition(cameraPosition, cameraRotation);

            if (_camera)
                _camera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);

            if (_renderOnTopCamera)
                _renderOnTopCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
        }

        private Vector3 GetCameraPositionForVirtualTarget()
        {
            var follow = _cinemachineCamera.GetComponent<CinemachineFollow>();

            if (!follow)
                return _virtualTarget.position;

            return _virtualTarget.position + follow.FollowOffset;
        }

        private void ResetCameraInstant()
        {
            if (_isArenaMap)
            {
                _virtualTarget.position = _levelSystem.CurrentCameraMapConfig.PositionForMap;
                _currentOrthoSize = _levelSystem.CurrentCameraMapConfig.OrthoSize;
                SetupBoundBox(_levelSystem.CurrentCameraMapConfig.PositionForMap, _currentOrthoSize);
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

        private void SetupBoundBox(Vector3 position, float orthoSize)
        {
            _maxArenaOrthoSize = orthoSize;
            _boundbox.transform.position = position + _cinemachineCamera.GetComponent<CinemachineFollow>().FollowOffset;
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
            //TODO Refacto en UniTask pour Victor
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

                float newOrtho = Mathf.Lerp(startOrtho, targetOrtho, t * t);
                _cinemachineCamera.Lens.OrthographicSize = newOrtho;
                _renderOnTopCamera.orthographicSize = newOrtho;
            }
            
            var cameraForward = zoomCineCam.transform.forward;
            cameraForward.y = 0;
            
            playerWin.transform.LookAt(cameraForward);
        }

        public void ResetToMainCamera()
        {
            zoomCineCam.gameObject.SetActive(false);
        }
    }
}