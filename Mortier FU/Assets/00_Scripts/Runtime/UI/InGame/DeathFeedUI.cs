using MortierFu.Shared;
using UnityEngine;
using UnityEngine.ResourceManagement.Exceptions;

namespace MortierFu
{
    public class DeathFeedUI : MonoBehaviour
    {
        [SerializeField] private GameObject _deathNotificationPrefab;
        [SerializeField] private RectTransform[] _slots;
        private bool[] _slotAvailability;
        
        private EventBinding<EventPlayerDeath> _playerDeathBinding;

        void Awake()
        {
            _slotAvailability = new bool[_slots.Length];
            for (int i = 0; i < _slots.Length; i++)
            {
                _slotAvailability[i] = true;
            }
        }

        void OnEnable()
        {
            _playerDeathBinding = new EventBinding<EventPlayerDeath>(CreateDeathNotification);
            EventBus<EventPlayerDeath>.Register(_playerDeathBinding);
        }

        void OnDisable() => EventBus<EventPlayerDeath>.Deregister(_playerDeathBinding);

        private void CreateDeathNotification(EventPlayerDeath evt)
        {
            var slot = GetAvailableSlot();
            
            var go = Instantiate(_deathNotificationPrefab, slot);
            var notif = go.GetComponent<PlayerDeathNotification>();

            if (!notif)
            {
                Logs.LogWarning("Could not retrieve the Player Death Notification script onto the instantiated prefab !");
                return;
            }

            notif.Initialize(this, evt.Character, evt.Context);
            
            Debug.Log($"Create notif bacause {evt.Character.Owner.PlayerIndex} died !");
        }
        
        private RectTransform GetAvailableSlot()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slotAvailability[i])
                {
                    _slotAvailability[i] = false;
                    return _slots[i];
                }
            }

            throw new OperationException("No available slot for creating a new player death notification !");
        }

        public void OnNotificationFinished(Transform notifTransform)
        {
            var slot = notifTransform.parent as RectTransform;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != slot) continue;
                _slotAvailability[i] = true;
            }
            
            Destroy(notifTransform.gameObject);
        }
    }
}