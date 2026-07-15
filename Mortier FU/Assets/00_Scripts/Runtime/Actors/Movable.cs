using System;
using UnityEngine;

namespace MortierFu
{
    public class Movable : MonoBehaviour, IInteractable
    {
        [SerializeField] private bool _isAutomatic = true;
        [SerializeField] private bool destroyOnEnd = true;
        [SerializeField] private bool _waitForRaceStart;

        public Transform _target;
        public float _speed;

        private Vector3 _startingPoint;
        private Vector3 _targetPoint;

        private bool _isActivated = false;
        private Action<Movable> _releaseCallback;

        private GameModeBase _gm;

        private void Start()
        {
            //récup le gamemode et lie la fonction ActiveMovement au début de la race et que le _waitForRaceStart est en true
            _gm = GameService.CurrentGameMode as GameModeBase;

            if (_gm == null)
                return;
            _gm.OnRacePlayerConfirmation += ActivateMovement;
            //SIMON
            
            if (!_target)
                return;

            InitializeMovementPoints();
        }
        //SIMON
        private void ActivateMovement()
        {
            _waitForRaceStart = false;
        }
        //SIMON

        private void FixedUpdate()
        {
            if (!_target || (!_isAutomatic && !_isActivated) || _waitForRaceStart)
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

        public void Interact(Vector3 contactPoint)
        {
            _isActivated = true;
        }

        public bool IsDashInteractable => true;
        public bool IsBombshellInteractable => false;
    }
}