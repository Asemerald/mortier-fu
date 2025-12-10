using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace MortierFu
{
    public class PlayerConfirmationUI : MonoBehaviour
    {
        [System.Serializable]
        public class PlayerSlot
        {
            public Image AButtonImage;
            public Image OkImage;
            [HideInInspector] public Tween ATween;
        }

        [Header("Player Slots (Blue, Green, Red, Yellow order)")]
        [SerializeField] private List<PlayerSlot> _playerSlots;

        [Header("Animation Settings")]
        [SerializeField] private float _pulseScale = 1.15f;
        [SerializeField] private float _pulseDuration = 0.45f;

        [Header("References")]
        [SerializeField] private GameObject _horizontalLayoutParent; 

        private int _activePlayerCount;

        private void Awake()
        {
            if (_horizontalLayoutParent != null)
                _horizontalLayoutParent.SetActive(false);
        }

        private void OnEnable()
        {
            var service = ServiceManager.Instance.Get<ConfirmationService>();
            if (service != null)
            {
                service.OnPlayerConfirmed += NotifyPlayerConfirmed;
                service.OnAllPlayersConfirmed += HideConfirmation;
            }
        }

        private void OnDisable()
        {
            var service = ServiceManager.Instance.Get<ConfirmationService>();
            if (service != null)
            {
                service.OnPlayerConfirmed -= NotifyPlayerConfirmed;
                service.OnAllPlayersConfirmed -= HideConfirmation;
            }
        }

        public void SetActivePlayerCount(int count)
        {
            _activePlayerCount = Mathf.Clamp(count, 1, _playerSlots.Count);
        }

        public void ShowConfirmation()
        {
            if (_horizontalLayoutParent != null)
                _horizontalLayoutParent.SetActive(true);

            StartAllAnimations(_activePlayerCount);
        }

        public void HideConfirmation()
        {
            if (_horizontalLayoutParent != null)
                _horizontalLayoutParent.SetActive(false);

            foreach (var slot in _playerSlots)
                if (slot.ATween.isAlive)
                    slot.ATween.Stop();
        }

        private void StartAllAnimations(int playerCount)
        {
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                var slot = _playerSlots[i];
                bool isActive = i < playerCount;

                slot.AButtonImage.gameObject.SetActive(isActive);
                slot.OkImage.gameObject.SetActive(false);

                if (slot.ATween.isAlive)
                    slot.ATween.Stop();

                if (isActive)
                {
                    slot.ATween = Tween.Scale(
                        target: slot.AButtonImage.rectTransform,
                        Vector3.one * _pulseScale,
                        duration: _pulseDuration,
                        ease: Ease.InOutQuad,
                        cycles: -1,
                        cycleMode: CycleMode.Yoyo
                    );
                }
            }
        }

        public void NotifyPlayerConfirmed(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= _playerSlots.Count)
                return;

            var slot = _playerSlots[playerIndex];

            if (slot.ATween.isAlive)
                slot.ATween.Stop();

            slot.AButtonImage.gameObject.SetActive(false);
            slot.OkImage.gameObject.SetActive(true);
        }

        public void ResetUI()
        {
            StartAllAnimations(_activePlayerCount);
        }
    }
}
