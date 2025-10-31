using UnityEngine;
using System;

namespace MortierFu
{
    public class AugmentPickup : MonoBehaviour
    {
        [Tooltip(
            "If true the pickup GameObject will be destroyed after being taken. Otherwise it will be left disabled.")]
        [SerializeField]
        private bool _destroyOnTaken = false;
        
        private DA_Augment _augmentData;
        
        private bool _isTaken;
        
        private Collider _collider;
        
        public bool HasAugment => _augmentData != null;

        public DA_Augment AugmentData => _augmentData;

        public event Action<PlayerCharacter> OnTaken;

        public void Initialize()
        {
            // GameSystem de jeu
        }
        
        public void SetAugment(DA_Augment augment)
        {
            _augmentData = augment;
        }

        private void Start()
        {
            _collider = GetComponent<Collider>();
        }
        
        private void Reset()
        {
            if (_collider != null)
                _collider.isTrigger = true;

            _isTaken = false;
            gameObject.SetActive(true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isTaken) return;

            var player = other.GetComponentInParent<PlayerCharacter>();
            if (player == null) return;

            TryTake(player);
        }

        public bool TryTake(PlayerCharacter player)
        {
            if (player == null) return false;
            if (_isTaken) return false;

            if (_augmentData == null)
            {
                Debug.LogWarning(
                    $"[AugmentPickup] No augment assigned on pickup '{name}'. Call SetAugment(...) after Instantiate.");
                return false;
            }

            _isTaken = true;
            gameObject.SetActive(false);

            try
            {
                player.AddAugment(_augmentData);

                OnTaken?.Invoke(player);

                if (_destroyOnTaken)
                {
                    Destroy(gameObject);
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _isTaken = false;
                return false;
            }
        }
    }
}
