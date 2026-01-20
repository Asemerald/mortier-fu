using UnityEngine;

namespace MortierFu {
    public class LevelTombSpawner : MonoBehaviour {
        [SerializeField] private GameObject[] _tombPrefabs;
        private EventBinding<EventPlayerDeath> _playerDeathBinding;
        
        private void OnEnable() {
            _playerDeathBinding = new EventBinding<EventPlayerDeath>(OnPlayerDeath);
            EventBus<EventPlayerDeath>.Register(_playerDeathBinding);
        }

        private void OnDisable() {
            EventBus<EventPlayerDeath>.Deregister(_playerDeathBinding);
        }

        private void OnPlayerDeath(EventPlayerDeath evt) {
            if (evt.Character == null) return;

            int index = evt.Character.Owner.PlayerIndex;
            
            if (index < 0 || index >= _tombPrefabs.Length)
                return;

            Debug.Log("Player died cause of " + evt.Context.DeathCause + " by " + evt.Context.Killer);
            
            switch (evt.Context.DeathCause) {
                case E_DeathCause.BombshellExplosion:
                {
                    var prefab = _tombPrefabs[index];
                    Instantiate(prefab, evt.Character.transform.position, Quaternion.identity, new InstantiateParameters() {
                        scene = gameObject.scene
                    });
                    break;
                }
                case E_DeathCause.Fall:
                {
                    // Spawn water prefab
                    break;
                }
                case E_DeathCause.VehicleCrash:
                {
                    //Play sfx
                    break;
                }
            }
        }
    }
}