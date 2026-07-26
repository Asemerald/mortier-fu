using UnityEngine;

namespace MortierFu
{
    public sealed class WorldSpaceUIOrthoCompensator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _target;

        [Header("Reference")]
        [SerializeField, Min(0.01f)] private float _referenceOrthographicSize = 14f;

        [Header("Compensation")]
        [SerializeField] private bool _compensateScale = true;
        [SerializeField] private bool _compensateLocalHeight = true;
        [SerializeField, Range(0.05f, 1f)] private float _minFactor = 0.25f;
        [SerializeField, Range(1f, 3f)] private float _maxFactor = 1f;

        private Vector3 _baseLocalScale;
        private Vector3 _baseLocalPosition;

        private CameraSystem _cameraSystem;
        private Camera _camera;

        private void Awake()
        {
            if (!_target)
                _target = transform;

            _baseLocalScale = _target.localScale;
            _baseLocalPosition = _target.localPosition;

            ResolveCamera();
            Apply();
        }

        private void OnEnable()
        {
            ResolveCamera();
            Apply();
        }

        private void LateUpdate()
        {
            if (!_camera)
                ResolveCamera();

            Apply();
        }

        private void ResolveCamera()
        {
            if (_camera)
                return;

            _cameraSystem = SystemManager.Instance?.Get<CameraSystem>();
            _camera = _cameraSystem?.Controller?.Camera;
        }

        private void Apply()
        {
            if (!_camera || !_target || !_camera.orthographic)
                return;

            float factor = _camera.orthographicSize / _referenceOrthographicSize;
            factor = Mathf.Clamp(factor, _minFactor, _maxFactor);

            if (_compensateScale)
                _target.localScale = _baseLocalScale * factor;

            if (_compensateLocalHeight)
            {
                _target.localPosition = new Vector3(
                    _baseLocalPosition.x,
                    _baseLocalPosition.y * factor,
                    _baseLocalPosition.z
                );
            }
        }
    }
}