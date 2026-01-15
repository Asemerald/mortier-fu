using UnityEngine;

namespace MortierFu
{
    public class Breakable : MonoBehaviour, IInteractable
    {
        [SerializeField] private int _life = 1;
        [SerializeField] private float _explosionForce = 50;
        [SerializeField] private float _explosionRadius = 5f;
        [SerializeField] private float _upwardsModifier = 1f;
        private bool _isIntact;
        [Space]
        [SerializeField] private GameObject _intactMesh;
        [SerializeField] private GameObject _shatteredMesh;

        void Awake()
        {
            _intactMesh.SetActive(true);
            _shatteredMesh.SetActive(false);

            _isIntact = true;
        }
        
        public void Interact(Vector3 contactPoint)
        {
            _life--;
            if (_life <= 0)
            {
                Destruct(contactPoint);
                return;
            }
        }

        protected virtual void Destruct(Vector3 contactPoint)
        {
            if (!_isIntact) return;
            _isIntact = false;
            
            _intactMesh.SetActive(false);
            _shatteredMesh.SetActive(true);
            
            // Get each shard's rigidbody
            var rigidbodies = _shatteredMesh.GetComponentsInChildren<Rigidbody>();
            
            foreach (var rb in rigidbodies)
            {
                rb.AddExplosionForce(_explosionForce, contactPoint, _explosionRadius, _upwardsModifier);
            }
        } 

        public bool IsDashInteractable => true;
        public bool IsBombshellInteractable => true;
    }   
}