using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class SnapPlayer : MonoBehaviour
    {
        #region Variables

        [SerializeField] private LayerMask _ignoreLayers;
        
        private Vector3 _lastPosition;
        private Vector3 _oldPivot;

        private readonly HashSet<PlayerCharacter> _playersOnPlatform = new();

        #endregion

        #region Unity LifeCycle

        void Awake()
        {
            _lastPosition = transform.position;
        }

        void FixedUpdate()
        {
            CalculDeltaPlatform();

            foreach (var player in _playersOnPlatform)
            {
                ApplyPlatformDelta(player.Controller);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer.Equals(_ignoreLayers)) return;
            
            PlayerCharacter player = other.GetComponentInParent<PlayerCharacter>();

            if (!player)
            {
                other.transform.SetParent(transform);
                Logs.Log($"Enter Element : {other.gameObject.name}",this);
                return;
            }

            _playersOnPlatform.Add(player);
        }

        void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer.Equals(_ignoreLayers)) return;
            
            PlayerCharacter player = other.GetComponentInParent<PlayerCharacter>();

            if (!player)
            {
                other.transform.SetParent(null);
                return;
            }

            _playersOnPlatform.Remove(player);
        }

        #endregion

        private void CalculDeltaPlatform()
        {
            _oldPivot = _lastPosition;
            _lastPosition = transform.position;
        }

        private void ApplyPlatformDelta(ControllerCharacterComponent controller)
        {
            Vector3 newPos = controller.rigidbody.position + (transform.position - _oldPivot);
            controller.rigidbody.MovePosition(newPos);
        }
    }
}