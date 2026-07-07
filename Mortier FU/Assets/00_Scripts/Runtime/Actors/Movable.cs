using System;
using UnityEngine;

namespace MortierFu
{
    public class Movable : MonoBehaviour, IInteractable
    {
        [SerializeField] private bool _isAutomatic = true;
        [SerializeField] private bool destroyOnEnd = true;

        public Transform _target;
        public float _speed;

        private Vector3 _startingPoint;
        private Vector3 _targetPoint;

        private bool _isActivated = false;
        private Action<Movable> _releaseCallback;

        private void Start()
        {
            if (!_target)
                return;

            InitializeMovementPoints();
        }

        private void Update()
        {
            if (!_target || (!_isAutomatic && !_isActivated))
                return;

            transform.position = Vector3.MoveTowards(transform.position, _targetPoint, _speed / 1000);

            if (transform.position != _targetPoint)
                return;

            if (destroyOnEnd)
            {
                if (_releaseCallback != null)
                    _releaseCallback.Invoke(this);
                else
                    Destroy(gameObject);
            }
            else
            {
                (_targetPoint, _startingPoint) = (_startingPoint, _targetPoint);
                _isActivated = false;
            }
        }

        public void Configure(Transform target, float speed)
        {
            _target = target;
            _speed = speed;

            if (!_target)
                return;

            InitializeMovementPoints();
        }

        public void SetReleaseCallback(Action<Movable> releaseCallback)
        {
            _releaseCallback = releaseCallback;
        }

        public void ResetForPool()
        {
            DetachPlayerChildren();

            _target = null;
            _startingPoint = Vector3.zero;
            _targetPoint = Vector3.zero;
            _isActivated = false;
        }

        private void InitializeMovementPoints()
        {
            _startingPoint = transform.position;
            _targetPoint = _target.position;
        }

        private void OnCollisionEnter(Collision other)
        {
            if (!other.gameObject.TryGetComponent(out PlayerCharacter character))
                return;

            character.transform.SetParent(gameObject.transform);
        }

        private void OnCollisionExit(Collision other)
        {
            if (!other.gameObject.TryGetComponent(out PlayerCharacter character))
                return;

            character.transform.SetParent(null);
        }

        private void DetachPlayerChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);

                if (!child.TryGetComponent(out PlayerCharacter character))
                    continue;

                character.transform.SetParent(null);
            }
        }

        public void Interact(Vector3 contactPoint)
        {
            _isActivated = true;
        }

        public bool IsDashInteractable => true;
        public bool IsBombshellInteractable => false;
    }
}