using System.Numerics;
using MortierFu.Shared;
using PrimeTween;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace MortierFu {
    public class LevelTombSpawner : MonoBehaviour {
        [SerializeField] private GameObject[] _tombPrefabs;
        [SerializeField] private GameObject[] _waterTombPrefabs;
        [SerializeField] private float _waterTombOffsetY = 1.5f;
        [SerializeField] private float _waterRaycastHeight = 10f;
        [SerializeField] private LayerMask _whatIsWater;
        [SerializeField] private TweenSettings _riseSettings;
        
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
                    var tomb = Instantiate(prefab, evt.Character.transform.position, Quaternion.identity, new InstantiateParameters() {
                        scene = gameObject.scene
                    });
                    
                    tomb.transform.localScale = evt.Character.transform.localScale;
                    
                    break;
                }
                case E_DeathCause.Fall:
                {
                    Vector3 characterPos = evt.Character.transform.position;
                    
                    if (Physics.Raycast(characterPos.Add(y: _waterRaycastHeight), Vector3.down, out RaycastHit hit,
                                        _waterRaycastHeight * 1.5f, _whatIsWater, 
                                        QueryTriggerInteraction.Collide))
                    {
                        var prefab = _waterTombPrefabs[index];
                        float scale = evt.Character.transform.localScale.y;
                        Quaternion rot = evt.Character.transform.rotation;
                        
                        var tomb = Instantiate(prefab, hit.point.Add(y: -_waterTombOffsetY * scale), rot, new InstantiateParameters()
                        {
                            scene = gameObject.scene
                        });

                        tomb.transform.localScale = Vector3.one * scale;

                        Tween.Position(tomb.transform, hit.point, _riseSettings);
                    }
                    
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