using NaughtyAttributes;
using PrimeTween;
using UnityEngine;

namespace MortierFu
{
    public class VehicleModel : MonoBehaviour
    {
        [SerializeField] private BoxCollider _collider;
        [SerializeField] private MeshRenderer _renderer;
        [SerializeField] private MeshFilter _filter;
        [SerializeField] private Transform _modelScaler;

        [SerializeField] private bool _engineShakeAnimation;
        [SerializeField] private ShakeSettings _engineShakeSettings = new()
        {
            strength = Vector3.one * 0.1f,
            cycles = -1,
        };

        private Tween _engineShake;

        [ReadOnly] public float Speed;
        [ReadOnly] public float Progress;
        [ReadOnly] public float HalfLength = 0.5f;

        public Rigidbody Rigidbody { get; private set; }

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            RefreshHalfLength();
        }

        private void OnEnable()
        {
            if (!_engineShakeAnimation || !_modelScaler)
                return;

            if (_engineShake.isAlive)
                _engineShake.Complete();

            _engineShake = Tween.ShakeScale(_modelScaler, _engineShakeSettings);
        }

        private void OnDisable()
        {
            if (_engineShake.isAlive)
                _engineShake.Complete();
        }

        public void ConfigureAsClone(VehicleModel source)
        {
            if (!source)
                return;

            if (_filter && source._filter)
            {
                _filter.sharedMesh = source._filter.sharedMesh;
                _filter.transform.localPosition = source._filter.transform.localPosition;
                _filter.transform.localRotation = source._filter.transform.localRotation;
                _filter.transform.localScale = source._filter.transform.localScale;
            }

            if (_renderer && source._renderer)
                _renderer.sharedMaterials = source._renderer.sharedMaterials;

            if (_collider && source._collider)
            {
                _collider.center = source._collider.center;
                _collider.size = source._collider.size;
            }

            if (_modelScaler && source._modelScaler)
            {
                _modelScaler.localPosition = source._modelScaler.localPosition;
                _modelScaler.localRotation = source._modelScaler.localRotation;
                _modelScaler.localScale = source._modelScaler.localScale;
            }

            RefreshHalfLength();
        }

        public void ResetRuntime()
        {
            Speed = 0f;
            Progress = 0f;

            if (!Rigidbody)
                return;

            if (Rigidbody.isKinematic)
                return;

            Rigidbody.angularVelocity = Vector3.zero;
        }

        private void RefreshHalfLength()
        {
            if (!_collider)
            {
                HalfLength = 0.5f;
                return;
            }

            float scale = Mathf.Abs(_collider.transform.lossyScale.z);
            HalfLength = Mathf.Max(0.1f, _collider.size.z * scale * 0.5f);
        }
    }
}