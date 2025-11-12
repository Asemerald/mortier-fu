using MortierFu.Shared;
using NaughtyAttributes;
using UnityEngine;

namespace MortierFu
{
    public class LevelReporter : MonoBehaviour
    {
        [Header("Level Design")]
        public Transform[] SpawnPoints;
        [Space]
        [SerializeField] private bool _isAugmentMap = false;
        [ShowIf("_isAugmentMap")]
        public Transform[] AugmentPoints;

        void Awake()
        {
            var levelSystem = SystemManager.Instance.Get<LevelSystem>();
            if (levelSystem == null)
            {
                Logs.LogError("[LevelReporter]: Couldn't fetch level system from the SystemManager !");
                return;
            }
            
            // When this LD is loaded, should bound itself to the LevelSystem
            levelSystem.BindReporter(this);
        }
        
        #if UNITY_EDITOR
        
        [Header("Debugging")]
        [SerializeField] private bool _enableDebug = true;
        [SerializeField] private Color _spawnPointColor = Color.dodgerBlue;
        [SerializeField] private Color _augmentPointColor = Color.softRed;
        [SerializeField] private float _widgetSize = 0.1f;
        
        [Button]
        private void AutoPopulate()
        {
            SpawnPoints = null;
            AugmentPoints = null;
            
            var spawnPoints = transform.Find("Spawn Points");
            if (spawnPoints != null)
            {
                if (spawnPoints.childCount > 0)
                {
                    SpawnPoints = new Transform[spawnPoints.childCount];
                    for (int i = 0; i < spawnPoints.childCount; i++)
                    {
                        SpawnPoints[i] = spawnPoints.GetChild(i);
                    }
                
                    Logs.Log("Successfully populated the spawn points.");
                } else
                {
                    Logs.LogWarning("Found no spawn points in the parent holder.");
                }
            }
            else {
                Logs.LogWarning("Couldn't find Spawn Points in the child hierarchy.");
            }
            
            var augmentPoints = transform.Find("Augment Points");
            if (augmentPoints != null)
            {
                if (augmentPoints.childCount > 0)
                {
                    AugmentPoints = new Transform[augmentPoints.childCount];
                    for (int i = 0; i < augmentPoints.childCount; i++)
                    {
                        AugmentPoints[i] = augmentPoints.GetChild(i);
                    }
                
                    Logs.Log("Successfully populated the augment points.");
                }
                else
                {
                    Logs.LogWarning("Found no augment points in the parent holder.");
                }
            }
            else {
                Logs.LogWarning("Couldn't find Augment Points in the child hierarchy.");
            }
        }
        
        void OnDrawGizmos()
        {
            if (_enableDebug == false) return;
            
            if (SpawnPoints != null && SpawnPoints.Length > 0)
            {
                Gizmos.color = _spawnPointColor;
                foreach (var spawnPoint in SpawnPoints)
                {
                    if (spawnPoint == null) continue;
                    Gizmos.DrawSphere(spawnPoint.position, _widgetSize);
                }
            }

            if (_isAugmentMap && AugmentPoints != null && AugmentPoints.Length > 0)
            {
                Gizmos.color = _augmentPointColor;
                foreach (var augmentPoint in AugmentPoints)
                {
                    if (augmentPoint == null) continue; 
                    Gizmos.DrawSphere(augmentPoint.position, _widgetSize);
                }   
            }
        }
        #endif
    }
}
