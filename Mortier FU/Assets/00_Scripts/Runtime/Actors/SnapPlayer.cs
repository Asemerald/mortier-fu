using System.Collections.Generic;
using UnityEngine;

namespace MortierFu
{
    public class SnapPlayer : MonoBehaviour
    {
        #region Variables

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