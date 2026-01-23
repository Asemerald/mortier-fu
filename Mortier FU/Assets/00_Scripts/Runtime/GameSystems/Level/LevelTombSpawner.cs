using MortierFu.Shared;
using PrimeTween;
using UnityEngine;

namespace MortierFu {
    public class LevelTombSpawner : MonoBehaviour {
        [SerializeField] private GameObject[] _tombPrefabs;
        [SerializeField] private GameObject[] _waterTombPrefabs;
        [SerializeField] private float _waterTombOffsetY = 1.5f;
        [SerializeField] private float _waterRiseDuration = 1.3f;
        [SerializeField] private float _waterRaycastHeight = 10f;
        [SerializeField] private Ease _waterRiseEase = Ease.OutCubic;
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
                    var prefab = _waterTombPrefabs[index];

                    Vector3 characterPos = evt.Character.transform.position;
                    
                    if (Physics.Raycast(characterPos.Add(y: _waterRaycastHeight), Vector3.down, out RaycastHit hit,
                                         _waterRaycastHeight * 1.5f, LayerMask.NameToLayer("Water"), 
                                         QueryTriggerInteraction.Collide))
                    {
                        var tomb = Instantiate(prefab, hit.point.Add(-_waterTombOffsetY), Quaternion.identity, new InstantiateParameters()
                        {
                            scene = gameObject.scene
                        });

                        Tween.Position(tomb.transform, characterPos, _waterRiseDuration, _waterRiseEase);
                    }
                    
                    Debug.DrawLine(characterPos.Add(y:  _waterRaycastHeight), characterPos.Add(y: _waterRaycastHeight * 1.5f), Color.red, 3f);
                    
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