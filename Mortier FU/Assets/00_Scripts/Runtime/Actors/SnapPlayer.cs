using System.Collections.Generic;
using UnityEngine;

namespace MortierFu
{
    public class SnapPlayer : MonoBehaviour
    {
        private Vector3 _lastPosition;
        private Quaternion _lastRotation;
        private readonly HashSet<PlayerCharacter> _playersOnPlatform = new();

        void Awake()
        {
            _lastPosition = transform.position;
            _lastRotation = transform.rotation;
        }

        void FixedUpdate()
        {
            Vector3 deltaPos = transform.position - _lastPosition;
            Quaternion deltaRot = transform.rotation * Quaternion.Inverse(_lastRotation);

            foreach (var player in _playersOnPlatform)
            {
                player.Controller.ApplyPlatformDelta(deltaPos, deltaRot, transform.position);
            }

            _lastPosition = transform.position;
            _lastRotation = transform.rotation;
        }

        void OnTriggerEnter(Collider other)
        {
            PlayerCharacter player = other.GetComponentInParent<PlayerCharacter>();

            if (!player) return;
            
            _playersOnPlatform.Add(player);
        }

        void OnTriggerExit(Collider other)
        {
            PlayerCharacter player = other.GetComponentInParent<PlayerCharacter>();

            if (!player) return;
            
            _playersOnPlatform.Remove(player);
        }
    }
}