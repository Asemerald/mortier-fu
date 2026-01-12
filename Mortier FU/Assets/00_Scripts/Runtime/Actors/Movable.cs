using UnityEngine;

namespace MortierFu
{
    public class Movable : MonoBehaviour, IInteractable
    {
        [SerializeField] private bool _isAutomatic = true;
        [SerializeField] private Transform _target;
        [SerializeField] private float _speed;
        
        private Vector3 _startingPoint;
        private Vector3 _targetPoint;

        private bool _isActivated = false;
    
        void Start()
        {
            _startingPoint = transform.position;
            _targetPoint = _target.position;
        }

        void Update()
        {
            if (!_target || (!_isAutomatic && !_isActivated)) return;
            
            transform.position = Vector3.MoveTowards(transform.position, _targetPoint, _speed/1000);
            
            if (transform.position != _targetPoint) return;
            (_targetPoint,_startingPoint) = (_startingPoint,_targetPoint);
            _isActivated = false;
        }
        
        private void OnCollisionEnter(Collision other)
        {
            if (!other.gameObject.TryGetComponent(out PlayerCharacter character)) return;
            
            character.transform.SetParent(gameObject.transform);
        }

        private void OnCollisionExit(Collision other)
        {
            if (!other.gameObject.TryGetComponent(out PlayerCharacter character)) return;
            
            character.transform.SetParent(null);
        }

        public void Interact()
        {
            _isActivated = true;
        }

        public bool IsDashInteractable => true;
        public bool IsBombshellInteractable => false;
    }
}