using System;
using FMOD.Studio;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class Rotator : MonoBehaviour
    {
        [SerializeField] private float _speed = 12f;
        [SerializeField] private bool calculatePhysics;
        
        [SerializeField] private bool canMoveInLoading;

        private Rigidbody _rb;

        public Vector3 TransposePoint(Vector3 localPoint, float time)
        {
            var angle = time * _speed;
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);

            return transform.position + rotation * localPoint;
        }

        private void OnEnable()
        {
            if (calculatePhysics)
                _rb = GetComponent<Rigidbody>();
            
            if (transform.GetComponentInChildren<Bumper>())
            {
                AudioService audioService = ServiceManager.Instance.Get<AudioService>();
                audioService.CreateMapInstance(AudioService.FMODEvents.SFX_Misc_TuktukEngine, transform.position);
            }
        }

        void FixedUpdate()
        {
            if (!canMoveInLoading)
                return;

            if (calculatePhysics)
                _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, _speed * Time.deltaTime, 0f));
            else
                transform.Rotate(0, 1 * Time.deltaTime * _speed, 0);
        }

        public void ActivateMovement() => canMoveInLoading = true;
    }
}