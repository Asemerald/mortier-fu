using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class RaceReporter : MonoBehaviour
    {
        [Header("Race Mode")]
        [SerializeField] private SO_RaceModeDefinition _raceModeDefinition;

        [Header("Player Spawns")]
        [SerializeField] private Transform _bullySpawnPoint;
        [SerializeField] private Transform[] _racerSpawnPoints;

        [Header("Augment Layout")]
        [SerializeField] private Transform _augmentPivot;
        [SerializeField] private Transform[] _augmentSpawnPoints;
        [SerializeField] private float _augmentRadius = 4f;

        public SO_RaceModeDefinition RaceModeDefinition => _raceModeDefinition;
        public Transform BullySpawnPoint => _bullySpawnPoint;
        public Transform AugmentPivot => _augmentPivot;
        public float AugmentRadius => Mathf.Max(0f, _augmentRadius);

        public bool IsFirstRound = true;

        private void Awake()
        {
            LevelSystem levelSystem = SystemManager.Instance?.Get<LevelSystem>();

            if (levelSystem == null)
            {
                Logs.LogError("[RaceReporter] Couldn't fetch LevelSystem.", this);
                return;
            }

            levelSystem.BindRaceReporter(this);
        }

        public Transform GetRacerSpawnPoint(int index)
        {
            if (_racerSpawnPoints == null || _racerSpawnPoints.Length == 0)
                return null;

            index %= _racerSpawnPoints.Length;

            if (index < 0)
                index += _racerSpawnPoints.Length;

            return _racerSpawnPoints[index];
        }

        public bool TryPopulateFixedAugmentPoints(Vector3[] outPoints)
        {
            if (outPoints == null)
                return false;

            if (_augmentSpawnPoints == null || _augmentSpawnPoints.Length == 0)
                return false;

            for (int i = 0; i < outPoints.Length; i++)
            {
                Transform point = _augmentSpawnPoints[i % _augmentSpawnPoints.Length];
                outPoints[i] = point ? point.position : transform.position;
            }

            return true;
        }

        public bool TryPopulateCircleAround(Vector3 center, Vector3[] outPoints)
        {
            if (outPoints == null || outPoints.Length == 0)
                return false;

            float radius = AugmentRadius;

            for (int i = 0; i < outPoints.Length; i++)
            {
                float angle = i * (2f * Mathf.PI / outPoints.Length);
                float x = center.x + Mathf.Cos(angle) * radius;
                float z = center.z + Mathf.Sin(angle) * radius;

                outPoints[i] = new Vector3(x, center.y, z);
            }

            return true;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;

            if (_bullySpawnPoint)
                Gizmos.DrawSphere(_bullySpawnPoint.position, 0.25f);

            Gizmos.color = Color.cyan;

            if (_racerSpawnPoints != null)
            {
                for (int i = 0; i < _racerSpawnPoints.Length; i++)
                {
                    if (_racerSpawnPoints[i])
                        Gizmos.DrawSphere(_racerSpawnPoints[i].position, 0.2f);
                }
            }

            Gizmos.color = Color.magenta;

            if (_augmentSpawnPoints != null)
            {
                for (int i = 0; i < _augmentSpawnPoints.Length; i++)
                {
                    if (_augmentSpawnPoints[i])
                        Gizmos.DrawSphere(_augmentSpawnPoints[i].position, 0.18f);
                }
            }

            if (_augmentPivot)
            {
                Gizmos.DrawWireSphere(_augmentPivot.position, AugmentRadius);
            }
        }
#endif
    }
}
