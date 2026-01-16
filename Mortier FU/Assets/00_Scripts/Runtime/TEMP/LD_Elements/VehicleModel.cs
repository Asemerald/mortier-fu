using NaughtyAttributes;
using UnityEngine;

namespace MortierFu
{
    /// <summary>
    /// Prototype design pattern
    /// </summary>
    public class VehicleModel : MonoBehaviour
    {
        [SerializeField] private BoxCollider _collider;
        [SerializeField] private MeshRenderer _renderer;
        [SerializeField] private MeshFilter _filter;
        [SerializeField] private TrailRenderer[] _trailRenderers;

        [ReadOnly]
        public float Speed;
        
        public Rigidbody Rigidbody { get; private set; }

        void Awake() => Rigidbody = GetComponent<Rigidbody>();
        
        public void ConfigureAsClone(VehicleModel source)
        {
            if (_filter && source._filter)
            {
                _filter.mesh = source._filter.sharedMesh;
                _filter.transform.localPosition = source._filter.transform.localPosition;
                _filter.transform.localScale = source._filter.transform.localScale;
            }
        
            if (_renderer && source._renderer)
            {
                _renderer.sharedMaterials = source._renderer.sharedMaterials;
            }

            if (_collider && source._collider)
            {
                _collider.center = source._collider.center;
                _collider.size = source._collider.size;
            }
        }
    }
}
