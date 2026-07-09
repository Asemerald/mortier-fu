using System.Collections.Generic;
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

            foreach (PlayerCharacter player in _playersOnPlatform)
            {
                if (player == null || player.Controller == null || player.Controller.rigidbody == null) continue;

                ApplyPlatformDelta(player.Controller);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other == null) return;
            if (((1 << other.gameObject.layer) & _ignoreLayers.value) != 0) return;

            PlayerCharacter player = other.GetComponentInParent<PlayerCharacter>();

            if (!player)
            {
                Transform t = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;

                if (t == null) return;

                if (t.parent != null && t.parent != transform && t.parent.GetComponent<SnapPlayer>() != null)
                    return;

                t.SetParent(transform, true);
                return;
            }

            _playersOnPlatform.Add(player);
        }

        void OnTriggerExit(Collider other)
        {
            if (other == null) return;
            if (((1 << other.gameObject.layer) & _ignoreLayers.value) != 0) return;

            PlayerCharacter player = other.GetComponentInParent<PlayerCharacter>();

            if (!player)
            {
                Transform t = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;

                if (t == null) return;

                if (t.parent == transform)
                    t.SetParent(null, true);

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
            if (controller == null || controller.rigidbody == null) return;

            Vector3 newPos = controller.rigidbody.position + (transform.position - _oldPivot);
            controller.rigidbody.MovePosition(newPos);
        }
    }
}