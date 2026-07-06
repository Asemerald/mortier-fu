using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MortierFu
{
    public class Breakable : MonoBehaviour, IInteractable
    {
        [SerializeField] protected int _life = 1;
        [SerializeField] private float _explosionForce = 50;
        [SerializeField] private float _explosionRadius = 5f;
        [SerializeField] private float _upwardsModifier = 1f;
        private bool _isIntact;
        [Space]
        [SerializeField] protected GameObject _intactMesh;
        [SerializeField] private GameObject _shatteredMesh;
        [SerializeField] protected float durationCleanUp = 1.5f;

        private readonly List<Rigidbody> _shatteredRbChildren = new();
        private readonly List<GameObject> _shatteredMeshChildren = new();

        private readonly CancellationTokenSource _cts = new();

        protected virtual void Awake()
        {
            if(_intactMesh)
                _intactMesh?.SetActive(true);
            
            if(_shatteredMesh)
                _shatteredMesh.SetActive(false);

            _isIntact = true;
            
            _shatteredMeshChildren.Clear();
            _shatteredRbChildren.Clear();
        }
        
        public virtual void Interact(Vector3 contactPoint)
        {
            _life--;
            if (_life > 0) return;
            AudioService.PlayBreakAudio(AudioService.FMODEvents.SFX_Misc_Break, contactPoint).Forget();
                
            Destruct(contactPoint);
        }

        protected virtual void Destruct(Vector3 contactPoint)
        {
            if (!_isIntact) return;
            _isIntact = false;

            if (!_intactMesh)
                return;

            if (!_shatteredMesh)
            {
                Destroy(_intactMesh);
                return;
            }
            
            _intactMesh.SetActive(false);
            _shatteredMesh.SetActive(true);
            
            // Get each shard's rigidbody
            var rigidbodies = _shatteredMesh.GetComponentsInChildren<Rigidbody>();
            
            _shatteredRbChildren.AddRange(rigidbodies);
            
            foreach (var rb in rigidbodies)
            {
                rb.AddExplosionForce(_explosionForce, contactPoint, _explosionRadius, _upwardsModifier);
            }
            
            ShatterPiecesCleanUp().Forget();
        } 

        public bool IsDashInteractable => true;
        public bool IsBombshellInteractable => true;
        

        private async UniTask ShatterPiecesCleanUp()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(2f));
            
            int count = _shatteredRbChildren.Count;
            
            float elapsedTime = 0f;

            foreach (Rigidbody rb in _shatteredRbChildren)
            {
                _shatteredMeshChildren.Add(rb.gameObject);
                Destroy(rb); //doesnt have any other option because of enabled doesnt exist with rb wtf ?
            }
            
            while (elapsedTime < durationCleanUp)
            {
                elapsedTime += Time.deltaTime;
                
                float t = elapsedTime / durationCleanUp;
                
                for (int i = 0; i < count; i++)
                {
                    Vector3 newScale = Vector3.Lerp(
                        _shatteredMeshChildren[i].transform.localScale,
                        Vector3.zero,
                        t);
                    
                    _shatteredMeshChildren[i].transform.localScale = newScale;
                }
                
                await UniTask.Yield(PlayerLoopTiming.Update,_cts.Token);
            }
            
            for (int i = 0; i < count; i++)
                _shatteredMeshChildren[i].transform.localScale = Vector3.zero;
            
            //for the moment I didnt Destroy the obj for performance issue like GC spike 
        }

        private void OnDestroy()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }   
}