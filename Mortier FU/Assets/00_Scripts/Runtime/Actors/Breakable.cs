using UnityEngine;

namespace MortierFu
{
    public class Breakable : MonoBehaviour, IInteractable
    {
        [SerializeField] private int _life = 1;
        [SerializeField] private Material _mat;
    
        private MeshRenderer _meshRenderer;

        private void Awake()
        {
            _meshRenderer = GetComponentInChildren<MeshRenderer>();
        }

        public void Interact()
        {
            _life--;
            if (_life <= 0)
            { 
                Destroy(gameObject); 
                return;
            }
        
            if(_mat) _meshRenderer.material = _mat;
        }

        public bool IsDashInteractable => true;
        public bool IsBombshellInteractable => true;
    }   
}