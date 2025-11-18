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
        public Transform AugmentPivot;
        public float Radius = 4f;

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
        [SerializeField] private Color _winnerSpawnColor = Color.yellow;
        [SerializeField] private Color _spawnPointColor = Color.dodgerBlue;
        [SerializeField] private Color _augmentPointColor = Color.softRed;
        [SerializeField] private float _widgetSize = 0.1f;
        
        [Button]
        private void AutoPopulate()
        {
            SpawnPoints = null;
            AugmentPivot = null;
            
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
            
            AugmentPivot = transform.Find("Augment Pivot");
            if (AugmentPivot == null)
            {
                Logs.LogWarning("Couldn't find Augment Points in the child hierarchy.");
            }
        }
        
        void OnDrawGizmos()
        {
            if (_enableDebug == false) return;
            
            if (SpawnPoints != null && SpawnPoints.Length > 0)
            {
                for (var i = 0; i < SpawnPoints.Length; i++)
                {
                    Gizmos.color = i == 0 ? _winnerSpawnColor : _spawnPointColor;
                
                    var spawnPoint = SpawnPoints[i];
                    if (spawnPoint == null) continue;
                    Gizmos.DrawSphere(spawnPoint.position, _widgetSize);
                }
            }

            if (_isAugmentMap && AugmentPivot != null)
            {
                Gizmos.color = _augmentPointColor;
                Gizmos.DrawWireSphere(AugmentPivot.position, Radius);
            }
        }
        #endif
    }
}
